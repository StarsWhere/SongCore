using System;
using System.Linq;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    // TODO: Remove missing characteristic. Might end up in wiped save data.
    internal class CustomCharacteristicsHook : IInitializable, IDisposable
    {
        private Hook _getBeatmapCharacteristicBySerializedNameHook = null!;
        private readonly List<Hook> _nativeHooks = new();
        private delegate bool ParseOriginal(string name, out BeatmapCharacteristic characteristic);
        private delegate bool ParseDetour(ParseOriginal original, string name, out BeatmapCharacteristic characteristic);
        private delegate ReadOnlySpan<BeatmapCharacteristic> AllOriginal();
        private delegate ReadOnlySpan<BeatmapCharacteristic> AllDetour(AllOriginal original);
        private BeatmapCharacteristic[] _all = Array.Empty<BeatmapCharacteristic>();

        public void Initialize()
        {
            _getBeatmapCharacteristicBySerializedNameHook = new Hook(typeof(BeatmapCharacteristicCollection).GetMethod(nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName))!, GetCustomCharacteristic, true);
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("BeatmapCharacteristicFromSerializedName")!, (ParseDetour)Parse, true));
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("GetAllBeatmapCharacteristics")!, (AllDetour)All, true));
            foreach (string name in new[] {"SerializedName", "CompoundIdPartName", "LocalizedName", "LocalizedHint", "NameLocalizationKey", "HintLocalizationKey"})
            {
                string method = name;
                _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod(method, new[]{typeof(BeatmapCharacteristic)})!,
                    (Func<Func<BeatmapCharacteristic,string>,BeatmapCharacteristic,string>)((original,id) =>
                    {
                        var definition=NativeCompatibility.Custom(id);if(definition==null)return original(id);
                        return method switch {"CompoundIdPartName"=>definition.compoundIdPartName,"LocalizedName" or "NameLocalizationKey"=>definition.characteristicNameLocalizationKey,"LocalizedHint" or "HintLocalizationKey"=>definition.descriptionLocalizationKey,_=>definition.serializedName};
                    }), true));
            }
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("SortingOrder")!, (Func<Func<BeatmapCharacteristic,int>,BeatmapCharacteristic,int>)((original,id)=>NativeCompatibility.Custom(id)?.sortingOrder??original(id)),true));
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("NumberOfColors")!, (Func<Func<BeatmapCharacteristic,int>,BeatmapCharacteristic,int>)((original,id)=>NativeCompatibility.Custom(id)?.numberOfColors??original(id)),true));
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("ContainsRotationEvents")!, (Func<Func<BeatmapCharacteristic,bool>,BeatmapCharacteristic,bool>)((original,id)=>NativeCompatibility.Custom(id)?.containsRotationEvents??original(id)),true));
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicExtensions).GetMethod("Requires360Movement")!, (Func<Func<BeatmapCharacteristic,bool>,BeatmapCharacteristic,bool>)((original,id)=>NativeCompatibility.Custom(id)?.requires360Movement??original(id)),true));
            _nativeHooks.Add(new Hook(typeof(BeatmapCharacteristicCollection).GetMethod("GetBeatmapCharacteristicIcon")!,
                (Func<Func<BeatmapCharacteristicCollection,BeatmapCharacteristic,UnityEngine.Sprite>,BeatmapCharacteristicCollection,BeatmapCharacteristic,UnityEngine.Sprite>)((original,collection,id)=>NativeCompatibility.Custom(id)?.icon??original(collection,id)),true));
        }

        public void Dispose()
        {
            _getBeatmapCharacteristicBySerializedNameHook.Dispose();
            foreach(var hook in _nativeHooks)hook.Dispose();_nativeHooks.Clear();
        }
        private bool Parse(ParseOriginal original,string name,out BeatmapCharacteristic id)
        {
            if(Collections.customCharacteristics.Any(c=>c.serializedName==name)){id=NativeCompatibility.CustomId(name);return true;}
            return original(name,out id);
        }
        private ReadOnlySpan<BeatmapCharacteristic> All(AllOriginal original)
        {
            if(_all.Length!=6+Collections.customCharacteristics.Count)
                _all=original().ToArray().Concat(Collections.customCharacteristics.Select(c=>NativeCompatibility.CustomId(c.serializedName))).Distinct().ToArray();
            return _all;
        }

        private BeatmapCharacteristicSO? GetCustomCharacteristic(Func<BeatmapCharacteristicCollection, string, BeatmapCharacteristicSO> original, BeatmapCharacteristicCollection instance, string serializedName)
        {
            var result = original(instance, serializedName);

            if (result != null)
            {
                return result;
            }

            var customCharacteristic = Collections.customCharacteristics.FirstOrDefault(c => c.serializedName == serializedName);
            return customCharacteristic != null ? customCharacteristic : Collections.customCharacteristics.FirstOrDefault(c => c.serializedName == "MissingCharacteristic");
        }
    }
}
