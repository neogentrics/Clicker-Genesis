using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ClickerGenesis.Save
{
    /// <summary>
    /// Local JSON file at Application.persistentDataPath (2026-08-08, per Save-System-Design.md
    /// §1/§3/§5/§7). This is THE playtesting-blocker fix — not the full 2026-08-06 vision (cloud
    /// save, social login, real encryption), which stays deferred as "layer 2" behind the
    /// ISaveStorage seam.
    ///
    /// Anti-tamper posture: base64 + XOR against a fixed key is LIGHT OBFUSCATION, explicitly NOT
    /// real security — it stops a casual "open save.json in Notepad and change the number" edit,
    /// nothing more. A determined tester can still defeat it trivially. Real encryption/signing is
    /// layer 2, sequenced for when real money (Talents/IAP) is actually on the line.
    /// </summary>
    public class LocalFileSaveStorage : ISaveStorage
    {
        private const string FileName = "save.json";

        // Deliberately not a secret worth protecting — see the anti-tamper note above.
        private static readonly byte[] ObfuscationKey = Encoding.UTF8.GetBytes("ClickerGenesisSave-NotRealEncryption-2026");

        private readonly string saveDirectory;
        private string SavePath => Path.Combine(saveDirectory, FileName);
        private string BackupPath => SavePath + ".bak";
        private string TempPath => SavePath + ".tmp";

        public LocalFileSaveStorage(string directory = null)
        {
            saveDirectory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        /// <summary>Falls back to a fresh SaveData on ANY failure (missing file = first launch,
        /// malformed JSON = corruption, unreadable = permissions) rather than crashing or
        /// soft-locking the game — per §5's explicit design call.</summary>
        public SaveData Load()
        {
            var loaded = TryLoadFrom(SavePath);
            if (loaded == null)
            {
                Debug.LogWarning("SaveSystem: primary save missing or unreadable, trying backup.");
                loaded = TryLoadFrom(BackupPath);
            }
            if (loaded == null)
            {
                Debug.Log("SaveSystem: no valid save found — starting fresh.");
                return new SaveData { saveVersion = SaveMigrator.CurrentVersion };
            }

            if (loaded.saveVersion < SaveMigrator.CurrentVersion)
                loaded = SaveMigrator.Migrate(loaded);

            return loaded;
        }

        private SaveData TryLoadFrom(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string obfuscated = File.ReadAllText(path);
                string json = Deobfuscate(obfuscated);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveSystem: failed to load '{path}': {e.Message}");
                return null;
            }
        }

        /// <summary>Write to a temp file, back up the previous save, then atomically swap the temp
        /// file in — per §5. Protects against a killed-mid-save (very real on Android, no
        /// guaranteed quit event) destroying the whole save file.</summary>
        public void Save(SaveData data)
        {
            data.saveVersion = SaveMigrator.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data);
            string obfuscated = Obfuscate(json);

            Directory.CreateDirectory(saveDirectory);
            File.WriteAllText(TempPath, obfuscated);

            if (File.Exists(SavePath))
            {
                try { File.Copy(SavePath, BackupPath, overwrite: true); }
                catch (Exception e) { Debug.LogWarning($"SaveSystem: failed to write backup: {e.Message}"); }
            }

            try
            {
                if (File.Exists(SavePath))
                    File.Replace(TempPath, SavePath, null);
                else
                    File.Move(TempPath, SavePath);
            }
            catch (Exception)
            {
                // Fallback for platforms where File.Replace isn't supported/reliable (some Android
                // filesystems) - less atomic, but still correct.
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TempPath, SavePath);
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

        /// <summary>Deletes save.json/.bak/.tmp if present (2026-08-08). Each file is removed
        /// independently with its own try/catch so a lock on one (unlikely, but real on some
        /// platforms if something else has it open) doesn't stop the others from being cleared.</summary>
        public void DeleteSave()
        {
            DeleteIfExists(SavePath);
            DeleteIfExists(BackupPath);
            DeleteIfExists(TempPath);
        }

        private static void DeleteIfExists(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"SaveSystem: failed to delete '{path}': {e.Message}"); }
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
