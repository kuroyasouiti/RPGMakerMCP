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
    /// RPGMaker asset management handler.
    /// Handles operations for images and sounds.
    /// </summary>
    public class RPGMakerAssetsHandler : BaseCommandHandler
    {
        public override string Category => "rpgMakerAssets";
        public override string Version => "1.0.0";

        private static readonly Dictionary<string, string> ImageCategories = new Dictionary<string, string>
        {
            { "characters", "Characters" },
            { "faces", "Faces" },
            { "objects", "Objects" },
            { "enemy", "Enemy" },
            { "sv_actors", "SV_Actors" },
            { "system", "System" },
            { "ui", "Ui" },
            { "titles1", "Titles1" },
            { "titles2", "Titles2" },
            { "pictures", "Pictures" },
            { "parallaxes", "Parallaxes" },
            { "animation", "Animation" },
            { "background", "Background" }
        };

        private static readonly Dictionary<string, string> SoundCategories = new Dictionary<string, string>
        {
            { "bgm", "BGM" },
            { "bgs", "BGS" },
            { "me", "ME" },
            { "se", "SE" }
        };

        public override IEnumerable<string> SupportedOperations => new[]
        {
            // Image operations
            "listImages",
            "getImageById",
            "getImages",
            "importImage",
            "exportImage",
            "deleteImage",
            // Sound operations
            "listSounds",
            "getSoundById",
            "getSounds",
            "importSound",
            "exportSound",
            "deleteSound",
            // Asset management
            "getAssetInfo",
            "organizeAssets",
            "validateAssets",
            "backupAssets",
            "restoreAssets"
        };

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                // Image operations
                "listImages" => ListImages(payload),
                "getImageById" => GetImageById(payload),
                "getImages" => GetImages(payload),
                "importImage" => ImportImage(payload),
                "exportImage" => ExportImage(payload),
                "deleteImage" => DeleteImage(payload),
                // Sound operations
                "listSounds" => ListSounds(payload),
                "getSoundById" => GetSoundById(payload),
                "getSounds" => GetSounds(payload),
                "importSound" => ImportSound(payload),
                "exportSound" => ExportSound(payload),
                "deleteSound" => DeleteSound(payload),
                // Asset management
                "getAssetInfo" => GetAssetInfo(),
                "organizeAssets" => OrganizeAssets(payload),
                "validateAssets" => ValidateAssets(),
                "backupAssets" => BackupAssets(payload),
                "restoreAssets" => RestoreAssets(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        protected override bool RequiresCompilationWait(string operation)
        {
            var readOnlyOperations = new[] {
                "listImages", "getImageById", "getImages",
                "listSounds", "getSoundById", "getSounds",
                "getAssetInfo", "validateAssets"
            };
            return !readOnlyOperations.Contains(operation);
        }

        #region Image Operations

        private object ListImages(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var imagesPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");

            if (!Directory.Exists(imagesPath))
            {
                return CreatePaginatedResponse("images", new List<Dictionary<string, object>>(), payload);
            }

            var images = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && ImageCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(imagesPath, ImageCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    images.AddRange(GetImageFileIds(categoryPath, ImageCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in ImageCategories)
                {
                    var categoryPath = Path.Combine(imagesPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        images.AddRange(GetImageFileIds(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreatePaginatedResponse("images", images, payload);
        }

        private List<Dictionary<string, object>> GetImageFileIds(string path, string category)
        {
            var result = new List<Dictionary<string, object>>();
            var files = Directory.GetFiles(path, "*.png")
                .Concat(Directory.GetFiles(path, "*.jpg"))
                .Concat(Directory.GetFiles(path, "*.jpeg"));

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

        private object GetImageById(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !ImageCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", ImageCategories.Keys)}");
            }

            var imagePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images", ImageCategories[category], filename);

            if (!File.Exists(imagePath))
            {
                throw new InvalidOperationException($"Image file not found: {filename} in category {category}");
            }

            var fileInfo = new FileInfo(imagePath);
            return CreateSuccessResponse(
                ("filename", filename),
                ("category", ImageCategories[category]),
                ("path", imagePath),
                ("size", fileInfo.Length),
                ("lastModified", fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            );
        }

        private object GetImages(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var imagesPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");

            if (!Directory.Exists(imagesPath))
            {
                return CreateSuccessResponse(("images", new List<object>()), ("message", "No images directory found."));
            }

            var images = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && ImageCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(imagesPath, ImageCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    images.AddRange(GetImageFiles(categoryPath, ImageCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in ImageCategories)
                {
                    var categoryPath = Path.Combine(imagesPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        images.AddRange(GetImageFiles(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreateSuccessResponse(
                ("images", images),
                ("count", images.Count)
            );
        }

        private List<Dictionary<string, object>> GetImageFiles(string path, string category)
        {
            var result = new List<Dictionary<string, object>>();
            var files = Directory.GetFiles(path, "*.png")
                .Concat(Directory.GetFiles(path, "*.jpg"))
                .Concat(Directory.GetFiles(path, "*.jpeg"));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                result.Add(new Dictionary<string, object>
                {
                    ["filename"] = Path.GetFileName(file),
                    ["category"] = category,
                    ["path"] = file,
                    ["size"] = fileInfo.Length,
                    ["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }

            return result;
        }

        private object ImportImage(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var category = GetString(payload, "category")?.ToLower();
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("Source path is required.");
            }

            if (string.IsNullOrEmpty(category) || !ImageCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", ImageCategories.Keys)}");
            }

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Source file does not exist: {sourcePath}");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFileName(sourcePath);
            }

            var targetDir = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images", ImageCategories[category]);
            Directory.CreateDirectory(targetDir);

            var targetPath = Path.Combine(targetDir, filename);
            File.Copy(sourcePath, targetPath, true);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("category", ImageCategories[category]),
                ("filename", filename),
                ("message", "Image imported successfully.")
            );
        }

        private object ExportImage(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();
            var targetPath = GetString(payload, "targetPath");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !ImageCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", ImageCategories.Keys)}");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images", ImageCategories[category], filename);

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Image file not found: {filename}");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
            }

            File.Copy(sourcePath, targetPath, true);

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("message", "Image exported successfully.")
            );
        }

        private object DeleteImage(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !ImageCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", ImageCategories.Keys)}");
            }

            var imagePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images", ImageCategories[category], filename);

            if (!File.Exists(imagePath))
            {
                throw new InvalidOperationException($"Image file not found: {filename}");
            }

            File.Delete(imagePath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Image '{filename}' deleted successfully."));
        }

        #endregion

        #region Sound Operations

        private object ListSounds(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            if (!Directory.Exists(soundsPath))
            {
                return CreatePaginatedResponse("sounds", new List<Dictionary<string, object>>(), payload);
            }

            var sounds = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && SoundCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(soundsPath, SoundCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    sounds.AddRange(GetSoundFileIds(categoryPath, SoundCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in SoundCategories)
                {
                    var categoryPath = Path.Combine(soundsPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        sounds.AddRange(GetSoundFileIds(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreatePaginatedResponse("sounds", sounds, payload);
        }

        private List<Dictionary<string, object>> GetSoundFileIds(string path, string category)
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

        private object GetSoundById(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !SoundCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", SoundCategories.Keys)}");
            }

            var soundPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", SoundCategories[category], filename);

            if (!File.Exists(soundPath))
            {
                throw new InvalidOperationException($"Sound file not found: {filename} in category {category}");
            }

            var fileInfo = new FileInfo(soundPath);
            return CreateSuccessResponse(
                ("filename", filename),
                ("category", SoundCategories[category]),
                ("path", soundPath),
                ("size", fileInfo.Length),
                ("lastModified", fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
            );
        }

        private object GetSounds(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category")?.ToLower();
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            if (!Directory.Exists(soundsPath))
            {
                return CreateSuccessResponse(("sounds", new List<object>()), ("message", "No sounds directory found."));
            }

            var sounds = new List<Dictionary<string, object>>();

            if (!string.IsNullOrEmpty(category) && SoundCategories.ContainsKey(category))
            {
                var categoryPath = Path.Combine(soundsPath, SoundCategories[category]);
                if (Directory.Exists(categoryPath))
                {
                    sounds.AddRange(GetSoundFiles(categoryPath, SoundCategories[category]));
                }
            }
            else
            {
                foreach (var categoryPair in SoundCategories)
                {
                    var categoryPath = Path.Combine(soundsPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        sounds.AddRange(GetSoundFiles(categoryPath, categoryPair.Value));
                    }
                }
            }

            return CreateSuccessResponse(
                ("sounds", sounds),
                ("count", sounds.Count)
            );
        }

        private List<Dictionary<string, object>> GetSoundFiles(string path, string category)
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
                    ["filename"] = Path.GetFileName(file),
                    ["category"] = category,
                    ["path"] = file,
                    ["size"] = fileInfo.Length,
                    ["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }

            return result;
        }

        private object ImportSound(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var category = GetString(payload, "category")?.ToLower();
            var filename = GetString(payload, "filename");

            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("Source path is required.");
            }

            if (string.IsNullOrEmpty(category) || !SoundCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", SoundCategories.Keys)}");
            }

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Source file does not exist: {sourcePath}");
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = Path.GetFileName(sourcePath);
            }

            var targetDir = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", SoundCategories[category]);
            Directory.CreateDirectory(targetDir);

            var targetPath = Path.Combine(targetDir, filename);
            File.Copy(sourcePath, targetPath, true);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("category", SoundCategories[category]),
                ("filename", filename),
                ("message", "Sound imported successfully.")
            );
        }

        private object ExportSound(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();
            var targetPath = GetString(payload, "targetPath");

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !SoundCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", SoundCategories.Keys)}");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", SoundCategories[category], filename);

            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Sound file not found: {filename}");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
            }

            File.Copy(sourcePath, targetPath, true);

            return CreateSuccessResponse(
                ("sourceFile", sourcePath),
                ("targetFile", targetPath),
                ("message", "Sound exported successfully.")
            );
        }

        private object DeleteSound(Dictionary<string, object> payload)
        {
            var filename = GetString(payload, "filename");
            var category = GetString(payload, "category")?.ToLower();

            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException("Filename is required.");
            }

            if (string.IsNullOrEmpty(category) || !SoundCategories.ContainsKey(category))
            {
                throw new InvalidOperationException($"Valid category is required. Available: {string.Join(", ", SoundCategories.Keys)}");
            }

            var soundPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds", SoundCategories[category], filename);

            if (!File.Exists(soundPath))
            {
                throw new InvalidOperationException($"Sound file not found: {filename}");
            }

            File.Delete(soundPath);
            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(("message", $"Sound '{filename}' deleted successfully."));
        }

        #endregion

        #region Asset Management

        private object GetAssetInfo()
        {
            var imagesPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");
            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");

            var imageStats = ImageCategories.ToDictionary(
                kv => kv.Key,
                kv => Directory.Exists(Path.Combine(imagesPath, kv.Value))
                    ? Directory.GetFiles(Path.Combine(imagesPath, kv.Value), "*")
                        .Count(f => Path.GetExtension(f).ToLower() is ".png" or ".jpg" or ".jpeg")
                    : 0
            );

            var soundStats = SoundCategories.ToDictionary(
                kv => kv.Key,
                kv => Directory.Exists(Path.Combine(soundsPath, kv.Value))
                    ? Directory.GetFiles(Path.Combine(soundsPath, kv.Value), "*")
                        .Count(f => Path.GetExtension(f).ToLower() is ".wav" or ".mp3" or ".ogg")
                    : 0
            );

            return CreateSuccessResponse(
                ("images", new Dictionary<string, object>
                {
                    ["totalCount"] = imageStats.Values.Sum(),
                    ["categories"] = imageStats
                }),
                ("sounds", new Dictionary<string, object>
                {
                    ["totalCount"] = soundStats.Values.Sum(),
                    ["categories"] = soundStats
                })
            );
        }

        private object OrganizeAssets(Dictionary<string, object> payload)
        {
            var organized = 0;

            var imagesPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");
            foreach (var category in ImageCategories.Values)
            {
                var categoryPath = Path.Combine(imagesPath, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    organized++;
                }
            }

            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");
            foreach (var category in SoundCategories.Values)
            {
                var categoryPath = Path.Combine(soundsPath, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    organized++;
                }
            }

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("directoriesCreated", organized),
                ("message", $"Assets organized. Created {organized} directories.")
            );
        }

        private object ValidateAssets()
        {
            var issues = new List<Dictionary<string, object>>();
            var validCount = 0;

            var imagesPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Images");
            if (Directory.Exists(imagesPath))
            {
                foreach (var categoryPair in ImageCategories)
                {
                    var categoryPath = Path.Combine(imagesPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        foreach (var file in Directory.GetFiles(categoryPath, "*", SearchOption.AllDirectories))
                        {
                            var ext = Path.GetExtension(file).ToLower();
                            if (ext is ".png" or ".jpg" or ".jpeg")
                            {
                                validCount++;
                            }
                            else if (ext != ".meta")
                            {
                                issues.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "invalid_image_format",
                                    ["file"] = file,
                                    ["category"] = categoryPair.Value,
                                    ["message"] = $"Unsupported image format: {ext}"
                                });
                            }
                        }
                    }
                }
            }

            var soundsPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage", "Sounds");
            if (Directory.Exists(soundsPath))
            {
                foreach (var categoryPair in SoundCategories)
                {
                    var categoryPath = Path.Combine(soundsPath, categoryPair.Value);
                    if (Directory.Exists(categoryPath))
                    {
                        foreach (var file in Directory.GetFiles(categoryPath, "*", SearchOption.AllDirectories))
                        {
                            var ext = Path.GetExtension(file).ToLower();
                            if (ext is ".wav" or ".mp3" or ".ogg")
                            {
                                validCount++;
                            }
                            else if (ext != ".meta")
                            {
                                issues.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "invalid_sound_format",
                                    ["file"] = file,
                                    ["category"] = categoryPair.Value,
                                    ["message"] = $"Unsupported sound format: {ext}"
                                });
                            }
                        }
                    }
                }
            }

            return CreateSuccessResponse(
                ("validAssets", validCount),
                ("issues", issues),
                ("issueCount", issues.Count),
                ("message", $"Asset validation completed. {validCount} valid assets, {issues.Count} issues found.")
            );
        }

        private object BackupAssets(Dictionary<string, object> payload)
        {
            var backupPath = GetString(payload, "backupPath");
            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"RPGMaker_Assets_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            var sourcePath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");

            if (!Directory.Exists(sourcePath))
            {
                throw new InvalidOperationException("RPGMaker Storage directory not found.");
            }

            Directory.CreateDirectory(backupPath);
            CopyDirectory(sourcePath, backupPath);

            return CreateSuccessResponse(
                ("sourcePath", sourcePath),
                ("backupPath", backupPath),
                ("message", "Assets backed up successfully.")
            );
        }

        private object RestoreAssets(Dictionary<string, object> payload)
        {
            var backupPath = GetString(payload, "backupPath");
            if (string.IsNullOrEmpty(backupPath))
            {
                throw new InvalidOperationException("Backup path is required.");
            }

            if (!Directory.Exists(backupPath))
            {
                throw new InvalidOperationException($"Backup directory not found: {backupPath}");
            }

            var targetPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");
            var currentBackupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"RPGMaker_Assets_Current_{DateTime.Now:yyyyMMdd_HHmmss}");

            if (Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(currentBackupPath);
                CopyDirectory(targetPath, currentBackupPath);
            }

            Directory.CreateDirectory(targetPath);
            CopyDirectory(backupPath, targetPath);

            AssetDatabase.Refresh();
            RefreshHierarchy();

            return CreateSuccessResponse(
                ("backupPath", backupPath),
                ("targetPath", targetPath),
                ("currentBackupPath", currentBackupPath),
                ("message", "Assets restored successfully.")
            );
        }

        #endregion

        #region Helper Methods

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
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
