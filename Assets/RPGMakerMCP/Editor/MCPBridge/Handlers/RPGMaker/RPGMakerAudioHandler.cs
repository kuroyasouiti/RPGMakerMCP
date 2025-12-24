using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.RPGMaker
{
    /// <summary>
    /// RPGMaker audio management handler.
    /// Handles operations for BGM, BGS, ME, SE, and audio settings.
    /// </summary>
    public class RPGMakerAudioHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerAudio";
        public override string Version => "1.0.0";

        private static readonly Dictionary<string, string> AudioCategories = new Dictionary<string, string>
        {
            { "bgm", "BGM" },
            { "bgs", "BGS" },
            { "me", "ME" },
            { "se", "SE" }
        };

        public override IEnumerable<string> SupportedOperations => new[]
        {
            // Audio list operations
            "listAudioFiles",
            "getAudioFileById",
            "getAudioList",
            // Playback operations
            "playBgm",
            "stopBgm",
            "playBgs",
            "stopBgs",
            "playMe",
            "playSe",
            "stopAllAudio",
            // Volume operations
            "setBgmVolume",
            "setBgsVolume",
            "setMeVolume",
            "setSeVolume",
            // Settings and file management
            "getAudioSettings",
            "updateAudioSettings",
            "importAudioFile",
            "exportAudioFile",
            "deleteAudioFile",
            "getAudioInfo"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                // Audio list operations
                "listAudioFiles" => ListAudioFiles(payload),
                "getAudioFileById" => GetAudioFileById(payload),
                "getAudioList" => GetAudioList(payload),
                // Playback operations
                "playBgm" => PlayBGM(payload),
                "stopBgm" => StopBGM(),
                "playBgs" => PlayBGS(payload),
                "stopBgs" => StopBGS(),
                "playMe" => PlayME(payload),
                "playSe" => PlaySE(payload),
                "stopAllAudio" => StopAllAudio(),
                // Volume operations
                "setBgmVolume" => SetBGMVolume(payload),
                "setBgsVolume" => SetBGSVolume(payload),
                "setMeVolume" => SetMEVolume(payload),
                "setSeVolume" => SetSEVolume(payload),
                // Settings and file management
                "getAudioSettings" => GetAudioSettings(),
                "updateAudioSettings" => UpdateAudioSettings(payload),
                "importAudioFile" => ImportAudioFile(payload),
                "exportAudioFile" => ExportAudioFile(payload),
                "deleteAudioFile" => DeleteAudioFile(payload),
                "getAudioInfo" => GetAudioInfo(),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] {
                "listAudioFiles", "getAudioFileById", "getAudioList",
                "getAudioSettings", "getAudioInfo"
            };
            return !readOnlyOperations.Contains(operation);
        }

        #region Audio List

        private object ListAudioFiles(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            if (!Directory.Exists(soundsPath))
            {
                return CreatePaginatedResponse("audioFiles", new List<Dictionary<string, object>>(), payload);
            }

            var audioFiles = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && AudioCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(soundsPath, AudioCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    audioFiles.AddRange(GetAudioFileIds(categoryPath, AudioCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in AudioCategories)
                {
                    var categoryPath = Path.Combine(soundsPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        audioFiles.AddRange(GetAudioFileIds(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreatePaginatedResponse("audioFiles", audioFiles, payload);
        }

        private List<Dictionary<string, object>> GetAudioFileIds(string path, string category)
        {
            var result = new List<Dictionary<string, object>>();
            var files = Directory.GetFiles(path, "*.wav")
                .Concat(Directory.GetFiles(path, "*.mp3"))
                .Concat(Directory.GetFiles(path, "*.ogg"));

            foreach (var file in files)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["id"] = Path.GetFileName(file),
                    ["filename"] = Path.GetFileName(file),
                    ["category"] = category
                });
            }

            return result;
        }

        private object GetAudioFileById(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !AudioCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", AudioCategories.Keys)}");
            }

            var audioPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", AudioCategories[category], filename);

            if (!File.Exists(audioPath))
            {
                throw new InvalidOperationException($"Audio file not found: {filename} in category {category}");
            }

            var fileInfo = new FileInfo(audioPath);
            return CreateSuccessResponse(
                ("filename", filename),
                ("category", AudioCategories[category]),
                ("path", audioPath),
                ("size", fileInfo.Length),
                ("extension", Path.GetExtension(audioPath)),
                ("lastModified", fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            );
        }

        private object GetAudioList(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            if (!Directory.Exists(soundsPath))
            {
                return CreateSuccessResponse(("audioFiles", new List<object>()), ("message", "No sounds directory found."));
            }

            var audioFiles = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && AudioCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(soundsPath, AudioCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    audioFiles.AddRange(GetAudioFiles(categoryPath, AudioCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in AudioCategories)
                {
                    var categoryPath = Path.Combine(soundsPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        audioFiles.AddRange(GetAudioFiles(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreateSuccessResponse(
                ("audioFiles", audioFiles),
                ("count", audioFiles.Count)
            );
        }

        private List<Dictionary<string, object>> GetAudioFiles(string path, string category)
        {
            var result = new List<Dictionary<string, object>>();
            var files = Directory.GetFiles(path, "*.wav")
                .Concat(Directory.GetFiles(path, "*.mp3"))
                .Concat(Directory.GetFiles(path, "*.ogg"));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                result.Add(new Dictionary<string, object>
                {
                    ["filename"] = Path.GetFileNameWithoutExtension(file),
                    ["fullName"] = Path.GetFileName(file),
                    ["category"] = category,
                    ["path"] = file,
                    ["size"] = fileInfo.Length,
                    ["extension"] = Path.GetExtension(file),
                    ["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }

            return result;
        }

        #endregion

        #region Playback Control

        private object PlayBGM(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var volume = GetFloat(payload, "volume", 1.0f);
            var pitch = GetFloat(payload, "pitch", 1.0f);
            var pan = GetFloat(payload, "pan", 0.0f);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var audioState = GetOrCreateAudioState();
            audioState["currentBGM"] = new Dictionary<string, object>
            {
                ["filename"] = filename,
                ["volume"] = volume,
                ["pitch"] = pitch,
                ["pan"] = pan,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            SaveAudioState(audioState);

            return CreateSuccessResponse(
                ("filename", filename),
                ("volume", volume),
                ("pitch", pitch),
                ("pan", pan),
                ("message", $"BGM '{filename}' playback started.")
            );
        }

        private object StopBGM()
        {
            var audioState = GetOrCreateAudioState();
            audioState["currentBGM"] = null;
            SaveAudioState(audioState);

            return CreateSuccessResponse(("message", "BGM stopped."));
        }

        private object PlayBGS(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var volume = GetFloat(payload, "volume", 1.0f);
            var pitch = GetFloat(payload, "pitch", 1.0f);
            var pan = GetFloat(payload, "pan", 0.0f);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            var audioState = GetOrCreateAudioState();
            audioState["currentBGS"] = new Dictionary<string, object>
            {
                ["filename"] = filename,
                ["volume"] = volume,
                ["pitch"] = pitch,
                ["pan"] = pan,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            SaveAudioState(audioState);

            return CreateSuccessResponse(
                ("filename", filename),
                ("volume", volume),
                ("pitch", pitch),
                ("pan", pan),
                ("message", $"BGS '{filename}' playback started.")
            );
        }

        private object StopBGS()
        {
            var audioState = GetOrCreateAudioState();
            audioState["currentBGS"] = null;
            SaveAudioState(audioState);

            return CreateSuccessResponse(("message", "BGS stopped."));
        }

        private object PlayME(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var volume = GetFloat(payload, "volume", 1.0f);
            var pitch = GetFloat(payload, "pitch", 1.0f);
            var pan = GetFloat(payload, "pan", 0.0f);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            Debug.Log($"Playing ME: {filename} (Volume: {volume}, Pitch: {pitch}, Pan: {pan})");

            return CreateSuccessResponse(
                ("filename", filename),
                ("volume", volume),
                ("pitch", pitch),
                ("pan", pan),
                ("message", $"ME '{filename}' played.")
            );
        }

        private object PlaySE(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var volume = GetFloat(payload, "volume", 1.0f);
            var pitch = GetFloat(payload, "pitch", 1.0f);
            var pan = GetFloat(payload, "pan", 0.0f);

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            Debug.Log($"Playing SE: {filename} (Volume: {volume}, Pitch: {pitch}, Pan: {pan})");

            return CreateSuccessResponse(
                ("filename", filename),
                ("volume", volume),
                ("pitch", pitch),
                ("pan", pan),
                ("message", $"SE '{filename}' played.")
            );
        }

        private object StopAllAudio()
        {
            var audioState = new Dictionary<string, object>
            {
                ["currentBGM"] = null,
                ["currentBGS"] = null,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            SaveAudioState(audioState);
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "All audio stopped."));
        }

        #endregion

        #region Volume Control

        private object SetBGMVolume(Dictionary<string, object> payload)
        {
            return SetCategoryVolume(payload, "BGM");
        }

        private object SetBGSVolume(Dictionary<string, object> payload)
        {
            return SetCategoryVolume(payload, "BGS");
        }

        private object SetMEVolume(Dictionary<string, object> payload)
        {
            return SetCategoryVolume(payload, "ME");
        }

        private object SetSEVolume(Dictionary<string, object> payload)
        {
            return SetCategoryVolume(payload, "SE");
        }

        private object SetCategoryVolume(Dictionary<string, object> payload, string category)
        {
            var volume = GetFloat(payload, "volume", -1);

            if (volume < 0 || volume > 1)
            {
                throw new InvalidOperationException("Volume must be a number between 0 and 1.");
            }

            var audioSettingsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System", "audiosettings.json");
            Dictionary<string, object> audioSettings;

            if (File.Exists(audioSettingsPath))
            {
                var content = File.ReadAllText(audioSettingsPath);
                audioSettings = MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                audioSettings = new Dictionary<string, object>();
                Directory.CreateDirectory(Path.GetDirectoryName(audioSettingsPath));
            }

            if (!audioSettings.ContainsKey("volumes") || audioSettings["volumes"] == null)
            {
                audioSettings["volumes"] = new Dictionary<string, object>();
            }

            var volumes = audioSettings["volumes"] as Dictionary<string, object>;
            volumes[category.ToLower()] = volume;

            File.WriteAllText(audioSettingsPath, MiniJson.Serialize(audioSettings));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"{category} volume set to {volume}."));
        }

        #endregion

        #region Audio Settings

        private object GetAudioSettings()
        {
            var audioSettingsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System", "audiosettings.json");

            if (!File.Exists(audioSettingsPath))
            {
                return CreateSuccessResponse(("settings", new Dictionary<string, object>
                {
                    ["volumes"] = new Dictionary<string, object>
                    {
                        ["bgm"] = 1.0f,
                        ["bgs"] = 1.0f,
                        ["me"] = 1.0f,
                        ["se"] = 1.0f
                    }
                }));
            }

            var content = File.ReadAllText(audioSettingsPath);
            var audioSettings = MiniJson.Deserialize(content);

            return CreateSuccessResponse(("settings", audioSettings));
        }

        private object UpdateAudioSettings(Dictionary<string, object> payload)
        {
            var settingsData = GetPayloadValue<Dictionary<string, object>>(payload, "settingsData");

            if (settingsData == null)
            {
                throw new InvalidOperationException("Settings data is required.");
            }

            var audioSettingsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "System", "audiosettings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(audioSettingsPath));

            File.WriteAllText(audioSettingsPath, MiniJson.Serialize(settingsData));
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", "Audio settings updated successfully."));
        }

        #endregion

        #region File Operations

        private object ImportAudioFile(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var category = GetString(payload, "category")?.ToLower();
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("Source path is required.");
            }

            if (string.IsNullOrEmpty(category) || !AudioCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", AudioCategories.Keys)}");
            }

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Source file does not exist: {sourcePath}");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFileName(sourcePath);
            }

            var targetDir = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", AudioCategories[category]);
            Directory.CreateDirectory(targetDir);

            var targetPath = Path.Combine(targetDir, filename);
            File.Copy(sourcePath, targetPath, true);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("category", AudioCategories[category]),
                ("filename", filename),
                ("message", "Audio file imported successfully.")
            );
        }

        private object ExportAudioFile(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();
            var targetPath = GetString(payload, "targetPath");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !AudioCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", AudioCategories.Keys)}");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", AudioCategories[category], filename);

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Audio file not found: {filename}");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
            }

            File.Copy(sourcePath, targetPath, true);

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("message", "Audio file exported successfully.")
            );
        }

        private object DeleteAudioFile(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !AudioCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", AudioCategories.Keys)}");
            }

            var audioPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", AudioCategories[category], filename);

            if (!File.Exists(audioPath))
            {
                throw new InvalidOperationException($"Audio file not found: {filename}");
            }

            File.Delete(audioPath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Audio file '{filename}' deleted successfully."));
        }

        #endregion

        #region Audio Info

        private object GetAudioInfo()
        {
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            var categoryStats = AudioCategories.ToDictionary(
                kv => kv.Key,
                kv => Directory.Exists(Path.Combine(soundsPath, kv.Value))
                    ? Directory.GetFiles(Path.Combine(soundsPath, kv.Value), "*")
                        .Count(f => Path.GetExtension(f).ToLower() is ".wav" or ".mp3" or ".ogg")
                    : 0
            );

            return CreateSuccessResponse(
                ("totalCount", categoryStats.Values.Sum()),
                ("categories", categoryStats),
                ("supportedFormats", new[] { "wav", "mp3", "ogg" })
            );
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, object> GetOrCreateAudioState()
        {
            var audioStatePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "audiostate.json");

            if (File.Exists(audioStatePath))
            {
                var content = File.ReadAllText(audioStatePath);
                return MiniJson.Deserialize(content) as Dictionary<string, object> ?? new Dictionary<string, object>();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(audioStatePath));
                return new Dictionary<string, object>();
            }
        }

        private void SaveAudioState(Dictionary<string, object> audioState)
        {
            var audioStatePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "SaveData", "audiostate.json");
            Directory.CreateDirectory(Path.GetDirectoryName(audioStatePath));
            File.WriteAllText(audioStatePath, MiniJson.Serialize(audioState));
            AssetDatabase.Refresh();
        }

        private T GetPayloadValue<T>(Dictionary<string, object> payload, string key) where T : class
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                return value as T;
            }
            return null;
        }

        private float GetFloat(Dictionary<string, object> payload, string key, float defaultValue = 0f)
        {
            if (payload != null && payload.TryGetValue(key, out var value))
            {
                if (value is double doubleVal) return (float)doubleVal;
                if (value is float floatVal) return floatVal;
                if (value is long longVal) return longVal;
                if (float.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }

        private void RefreshHierarchy()
        {
            try
            {
                var hierarchyType = Type.GetType("RPGMaker.Codebase.Editor.Hierarchy.Hierarchy, Assembly-CSharp-Editor");
                if (hierarchyType != null)
                {
                    var refreshMethod = hierarchyType.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    refreshMethod?.Invoke(null, null);
                }
            }
            catch
            {
                // Ignore if hierarchy refresh is not available
            }
        }

        #endregion
    }
}
