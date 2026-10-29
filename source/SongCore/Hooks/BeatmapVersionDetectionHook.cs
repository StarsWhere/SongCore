using System;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This reverses base game logic so it first looks for V3 format instead of V2.
    /// Without this, maps that declare both <c>version</c> and <c>_version</c> will be empty.
    /// It also makes it much faster than the original implementation.
    /// </summary>
    internal class BeatmapVersionDetectionHook : IInitializable, IDisposable
    {
        private const string VersionSearchString = "version";

        private Hook _getVersionHook = null!;

        public void Initialize()
        {
            _getVersionHook = new Hook(typeof(BeatmapSaveDataHelpers).GetMethod(nameof(BeatmapSaveDataHelpers.GetVersion))!, GetVersion, true);
        }

        public void Dispose()
        {
            _getVersionHook.Dispose();
        }

        private Version GetVersion(Func<string, Version> original, string data)
        {
            Version? legacy = null;
            using var input = new System.IO.StringReader(data);
            using var reader = new Newtonsoft.Json.JsonTextReader(input);
            while (reader.Read())
            {
                if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || reader.Depth != 1) continue;
                string? name = reader.Value as string;
                if (!reader.Read()) break;
                if (name == "version" && Version.TryParse(reader.Value as string, out var modern)) return modern;
                if (name == "_version" && Version.TryParse(reader.Value as string, out var version)) legacy = version;
                reader.Skip();
            }
            return legacy ?? BeatmapSaveDataHelpers.noVersion;
        }
    }
}
