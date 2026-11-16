using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eldoria.Network;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Gameplay
{
    /// <summary>
    /// A saved army list on disk: its name and the army (faction, units and loadouts by asset name, the same format
    /// multiplayer sends; see <see cref="ArmyMsg"/>). Stored as JSON, one file per army.
    /// </summary>
    [Serializable]
    public class ArmyFile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string name;
        public ArmyMsg army = new ArmyMsg();
    }

    /// <summary>A saved army as listed in the army builder.</summary>
    public class SavedArmy
    {
        public string Name;
        public string Path;
        public string FactionName;
        public int UnitCount;
        public DateTime Saved;
        /// <summary>Why the file can't be read (null if it can).</summary>
        public string Error;
    }

    /// <summary>
    /// Saving and loading army lists as files (<see cref="Extension"/>) in <see cref="Folder"/>: the Armies folder in the
    /// game's save data. Files can be copied between players; units, factions and loadouts are matched by asset name.
    /// </summary>
    public static class ArmyFiles
    {
        public const string Extension = ".army.json";
        public const int MaxNameLength = 40;

        private static string folder;

        /// <summary>Where armies are saved (Application.persistentDataPath/Armies unless set, e.g. by tests).</summary>
        public static string Folder
        {
            get => Path.GetFullPath(folder ?? Path.Combine(Application.persistentDataPath, "Armies"));
            set => folder = value;
        }

        /// <summary>The name cleaned up for use as a file name (characters a file name can't have are dropped). Empty if nothing's left.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var bad = Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(c => !bad.Contains(c) && !char.IsControl(c)).ToArray()).Trim().TrimEnd('.');
            return clean.Length > MaxNameLength ? clean.Substring(0, MaxNameLength).TrimEnd() : clean;
        }

        public static string PathFor(string name) => Path.Combine(Folder, CleanName(name) + Extension);

        public static bool Exists(string name) => CleanName(name).Length > 0 && File.Exists(PathFor(name));

        /// <summary>Saves the army under <paramref name="name"/> (replacing any army of that name). Returns null if saved; otherwise why not.</summary>
        public static string Save(string name, Faction faction, IReadOnlyList<UnitTemplate> army, IReadOnlyList<UnitLoadout> loadouts)
        {
            string clean = CleanName(name);
            if (clean.Length == 0) return "Give the army a name";
            if (faction == null) return "Choose a faction first";
            if (army == null || army.Count == 0) return "The army is empty";
            var file = new ArmyFile { name = clean, army = ArmyMsg.From(faction, army, loadouts) };
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(PathFor(clean), JsonUtility.ToJson(file, true));
                return null;
            }
            catch (Exception e)
            {
                return $"Couldn't save: {e.Message}";
            }
        }

        /// <summary>Reads a saved army file. Null (with a reason) if it can't be read.</summary>
        public static ArmyFile Read(string path, out string error)
        {
            error = null;
            try
            {
                var file = JsonUtility.FromJson<ArmyFile>(File.ReadAllText(path));
                if (file == null || file.army == null || string.IsNullOrEmpty(file.army.faction)) { error = "Not an army file"; return null; }
                if (file.version > ArmyFile.CurrentVersion) { error = "Saved by a newer version of the game"; return null; }
                if (string.IsNullOrWhiteSpace(file.name)) file.name = Path.GetFileName(path).Replace(Extension, "");
                return file;
            }
            catch (Exception e)
            {
                error = e is ArgumentException ? "Not an army file" : $"Couldn't read: {e.Message}";
                return null;
            }
        }

        /// <summary>
        /// Reads a saved army and matches it against the catalog. False (with a reason) if the file can't be read or names
        /// a faction, unit or loadout choice the game doesn't have.
        /// </summary>
        public static bool TryLoad(string path, UnitCatalog catalog, out Faction faction, out List<UnitTemplate> army,
            out List<UnitLoadout> loadouts, out string error)
        {
            faction = null;
            army = null;
            loadouts = null;
            var file = Read(path, out error);
            if (file == null) return false;
            if (catalog == null) { error = "No unit catalog"; return false; }
            return file.army.TryResolve(catalog, out faction, out army, out loadouts, out error);
        }

        /// <summary>The saved armies, most recently saved first.</summary>
        public static List<SavedArmy> List(UnitCatalog catalog = null)
        {
            var list = new List<SavedArmy>();
            if (!Directory.Exists(Folder)) return list;
            foreach (var path in Directory.GetFiles(Folder, "*" + Extension))
            {
                var file = Read(path, out var error);
                var factionName = file?.army.faction;
                var faction = catalog != null && file != null ? catalog.Factions.FirstOrDefault(f => f.name == file.army.faction) : null;
                list.Add(new SavedArmy
                {
                    Name = file?.name ?? Path.GetFileName(path).Replace(Extension, ""),
                    Path = path,
                    FactionName = faction != null ? faction.displayName : factionName ?? "",
                    UnitCount = file?.army.units.Count ?? 0,
                    Saved = File.GetLastWriteTime(path),
                    Error = error,
                });
            }
            return list.OrderByDescending(a => a.Saved).ThenBy(a => a.Name).ToList();
        }

        /// <summary>Deletes a saved army file. Returns null if deleted; otherwise why not.</summary>
        public static string Delete(string path)
        {
            try
            {
                if (File.Exists(path) && path.EndsWith(Extension)) File.Delete(path);
                return null;
            }
            catch (Exception e)
            {
                return $"Couldn't delete: {e.Message}";
            }
        }
    }
}
