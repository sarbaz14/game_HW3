using System.IO;
using UnityEngine;

namespace ChickenShooter
{
    /// <summary>JSON persistence in Application.persistentDataPath/savegame.json.</summary>
    public static class SaveSystem
    {
        public const string FileName = "savegame.json";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSaveFile()
        {
            try
            {
                return File.Exists(SaveFilePath);
            }
            catch
            {
                return false;
            }
        }

        public static void Save(SaveData data)
        {
            if (data == null) return;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log("[SaveSystem] Saved progress to " + SaveFilePath + "\n" + json);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[SaveSystem] Failed to write save file: " + exception.Message);
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SaveFilePath)) return null;

                string json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json)) return null;

                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;

                data.levelIndex = Mathf.Clamp(data.levelIndex, 1, 3);
                data.roundIndex = Mathf.Max(1, data.roundIndex);
                data.remainingLives = Mathf.Max(1, data.remainingLives);
                data.secondaryMissiles = Mathf.Max(0, data.secondaryMissiles);
                data.weaponPowerLevel = Mathf.Clamp(data.weaponPowerLevel, 1, 3);
                data.foodProgress = Mathf.Clamp(data.foodProgress, 0f, 100f);
                return data;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[SaveSystem] Could not read save file: " + exception.Message);
                return null;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[SaveSystem] Save file deleted (run finished).");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[SaveSystem] Failed to delete save file: " + exception.Message);
            }
        }
    }
}
