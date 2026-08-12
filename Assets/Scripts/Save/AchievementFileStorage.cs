using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Local JSON file at Application.persistentDataPath, separate from save.json - one file PER
    /// SAVE SLOT (REVISED 2026-08-10, was a single global ledger shared across every slot - real
    /// user reversal: a global ledger made "start over" impossible, since a fresh slot or a
    /// deleted save would still show every achievement already earned on another slot. Achievement
    /// unlocks are now bound to the slot they were earned on, same as everything else in that
    /// slot's save). Still deliberately its own file rather than a section of SaveData (keeps the
    /// achievement ledger's own versioning/migration chain independent), just no longer a SINGLE
    /// shared file - see the slotIndex constructor parameter, same pattern as
    /// LocalFileSaveStorage. Same atomic-write/.bak-fallback/light-obfuscation discipline as
    /// LocalFileSaveStorage - a second, independent instance of that same pattern, not a new one
    /// invented from scratch.
    /// </summary>
    public class AchievementFileStorage : IAchievementStorage
    {
        // Slot-aware (2026-08-10, mirrors LocalFileSaveStorage exactly) - slot 0 keeps the legacy
        // "achievements.json" filename specifically so achievements earned before the per-slot
        // reversal keep loading as Slot 1's ledger instead of silently vanishing.
        private readonly string fileName;

        // Deliberately not a secret worth protecting - same posture as LocalFileSaveStorage's key.
        private static readonly byte[] ObfuscationKey = Encoding.UTF8.GetBytes("ClickerGenesisAchievements-NotRealEncryption-2026");

        private readonly string saveDirectory;
        private string DataPath => Path.Combine(saveDirectory, fileName);
        private string BackupPath => DataPath + ".bak";
        private string TempPath => DataPath + ".tmp";

        /// <param name="slotIndex">Which of the SaveSlotManager.SlotCount slots this instance
        /// reads/writes. Defaults to 0 (== "achievements.json", the pre-reversal filename) so any
        /// existing caller that doesn't pass a slot keeps working unchanged.</param>
        public AchievementFileStorage(int slotIndex = 0, string directory = null)
        {
            saveDirectory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            fileName = slotIndex <= 0 ? "achievements.json" : $"achievements_slot{slotIndex}.json";
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

        /// <summary>Deletes this slot's achievements.json/.bak/.tmp if present (2026-08-10) - the
        /// per-slot achievement-reversal counterpart to LocalFileSaveStorage.DeleteSave(), called
        /// alongside it from Settings' "Delete Saved Game" reset so earned achievements are wiped
        /// too, matching the "starting over really starts over" goal.</summary>
        public void DeleteSave()
        {
            DeleteIfExists(DataPath);
            DeleteIfExists(BackupPath);
            DeleteIfExists(TempPath);
        }

        private static void DeleteIfExists(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"AchievementSystem: failed to delete '{path}': {e.Message}"); }
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
