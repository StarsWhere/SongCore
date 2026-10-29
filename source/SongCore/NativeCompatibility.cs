namespace SongCore
{
    public static partial class NativeCompatibility
    {
        internal static BeatmapCharacteristic CustomId(string name)
        {
            if (name == "MissingCharacteristic") return (BeatmapCharacteristic)1000;
            if (name == "Lightshow") return (BeatmapCharacteristic)1001;
            if (name == "Lawless") return (BeatmapCharacteristic)1002;
            uint hash = 2166136261;
            foreach (char value in name) { hash ^= value; hash *= 16777619; }
            return (BeatmapCharacteristic)(10000 + (hash & 0x3fffffff));
        }
        internal static BeatmapCharacteristicSO? Custom(BeatmapCharacteristic id) =>
            System.Linq.Enumerable.FirstOrDefault(Collections.customCharacteristics, c => CustomId(c.serializedName) == id);
        public static string CharacteristicName(BeatmapCharacteristic characteristic) =>
            Custom(characteristic)?.serializedName ?? characteristic.ToString().Replace("Degree360", "360Degree").Replace("Degree90", "90Degree");
    }
}
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }
