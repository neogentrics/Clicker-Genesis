using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Local JSON file at Application.persistentDataPath, separate from save.json - the GLOBAL
    /// achievement ledger (2026-08-08). Deliberately its own file rather than a section of
    /// SaveData: achievement unlocks must persist across every save slot, so they can't live
    /// inside any one slot's save file. Same atomic-write/.bak-fallback/light-obfuscation
    /// discipline as LocalFileSaveStorage - a second, independent instance of that same pattern,
    /// not a new one invented from scratch. Not deleted by the Settings screen's "Delete Saved
    /// Game" reset (that only touches save.json via ISaveStorage.DeleteSave) - achievements are
    /// meta-progression, same as a console's platform achievements surviving a local save wipe.
    /// </summary>
    public class AchievementFileStorage : IAchievementStorage
    {
        private const string FileName = "achievements.json";

        // Deliberately not a secret worth protecting - same posture as LocalFileSaveStorage's key.
        private static readonly byte[] ObfuscationKey = Encoding.UTF8.GetBytes("ClickerGenesisAchievements-NotRealEncryption-2026");

        private readonly string saveDirectory;
        private string DataPath => Path.Combine(saveDirectory, FileName);
        private string BackupPath => DataPath + ".bak";
        private string TempPath => DataPath + ".tmp";

        public AchievementFileStorage(string directory = null)
        {
            saveDirectory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        public AchievementData Load()
        {
            var loaded = TryLoadFrom(DataPath);
            if (loaded == null)
            {
                Debug.LogWarning("AchievementSystem: primary ledger missing or unreadable, trying backup.");
                loaded = TryLoadFrom(BackupPath);
            }
            if (loaded == null)
            {
                Debug.Log("AchievementSystem: no valid ledger found - starting fresh.");
                return new AchievementData { dataVersion = AchievementDataMigrator.CurrentVersion };
            }

            if (loaded.dataVersion < AchievementDataMigrator.CurrentVersion)
                loaded = AchievementDataMigrator.Migrate(loaded);

            return loaded;
        }

        private AchievementData TryLoadFrom(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string obfuscated = File.ReadAllText(path);
                string json = Deobfuscate(obfuscated);
                return JsonUtility.FromJson<AchievementData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AchievementSystem: failed to load '{path}': {e.Message}");
                return null;
            }
        }

        public void Save(AchievementData data)
        {
            data.dataVersion = AchievementDataMigrator.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data);
            string obfuscated = Obfuscate(json);

            Directory.CreateDirectory(saveDirectory);
            File.WriteAllText(TempPath, obfuscated);

            if (File.Exists(DataPath))
            {
                try { File.Copy(DataPath, BackupPath, overwrite: true); }
                catch (Exception e) { Debug.LogWarning($"AchievementSystem: failed to write backup: {e.Message}"); }
            }

            try
            {
                if (File.Exists(DataPath))
                    File.Replace(TempPath, DataPath, null);
                else
                    File.Move(TempPath, DataPath);
            }
            catch (Exception)
            {
                if (File.Exists(DataPath)) File.Delete(DataPath);
                File.Move(TempPath, DataPath);
            }
        }

        private static string Obfuscate(string plainJson)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainJson);
            return Convert.ToBase64String(Xor(bytes, ObfuscationKey));
        }

        private static string Deobfuscate(string obfuscatedText)
        {
            byte[] bytes = Convert.FromBase64String(obfuscatedText);
            return Encoding.UTF8.GetString(Xor(bytes, ObfuscationKey));
        }

        private static byte[] Xor(byte[] data, byte[] key)
        {
            var result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            return result;
        }
    }
}
