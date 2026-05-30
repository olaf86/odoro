using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Odoro
{
    public sealed class AvatarAssetStore
    {
        private const string DefaultAvatarResourcePath = "Odoro/DefaultAvatar";
        private const string AvatarResourcesPath = "Odoro/Avatars";
        private const string AvatarStorageConfigResourcePath = "Odoro/AvatarStorage";
        private const string AvatarAssetsDirectoryName = "OdoroAvatarAssets";

        [Serializable]
        private sealed class AvatarStorageConfig
        {
            public string remoteAvatarStorageBaseUrl;
        }

        [Serializable]
        private sealed class AvatarPackageManifest
        {
            public int schemaVersion;
            public string avatarId;
            public string displayName;
            public string sourceFilename;
            public string runtimeAssetFilename;
            public long sourceFileByteCount;
            public string installedAtUtc;
        }

        private readonly string baseDirectoryPath;
        private readonly StageAvatarOption[] remoteCatalogOptions;

        public AvatarAssetStore(string baseDirectoryPath = null, string remoteAvatarStorageBaseUrl = null)
        {
            this.baseDirectoryPath = string.IsNullOrEmpty(baseDirectoryPath)
                ? Path.Combine(Application.persistentDataPath, AvatarAssetsDirectoryName)
                : baseDirectoryPath;
            var resolvedRemoteAvatarStorageBaseUrl = NormalizeBaseUrl(
                string.IsNullOrWhiteSpace(remoteAvatarStorageBaseUrl)
                    ? LoadConfiguredRemoteAvatarStorageBaseUrl()
                    : remoteAvatarStorageBaseUrl
            );
            remoteCatalogOptions = BuildRemoteCatalogOptions(resolvedRemoteAvatarStorageBaseUrl);
        }

        public IReadOnlyList<StageAvatarOption> FetchAvailableOptions()
        {
            var options = new List<StageAvatarOption>
            {
                new StageAvatarOption
                {
                    id = "procedural-skeleton",
                    kind = StageAvatarOptionKind.ProceduralSkeleton,
                    title = "Skeleton Preview",
                    subtitle = "Use the lightweight joint preview for motion checking.",
                },
            };

            AddResourceOption(options, DefaultAvatarResourcePath, "Default Avatar");
            var prefabs = Resources.LoadAll<GameObject>(AvatarResourcesPath);
            Array.Sort(prefabs, (left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            for (var prefabIndex = 0; prefabIndex < prefabs.Length; prefabIndex += 1)
            {
                var prefab = prefabs[prefabIndex];
                AddResourceOption(options, $"{AvatarResourcesPath}/{prefab.name}", DisplayName(prefab.name));
            }

            var installedOptions = new Dictionary<string, StageAvatarOption>();
            foreach (var installedOption in FetchInstalledDevelopmentAvatars())
            {
                installedOptions[installedOption.id] = installedOption;
            }

            for (var remoteIndex = 0; remoteIndex < remoteCatalogOptions.Length; remoteIndex += 1)
            {
                var remoteOption = remoteCatalogOptions[remoteIndex];
                if (installedOptions.TryGetValue(remoteOption.id, out var installedOption))
                {
                    options.Add(installedOption);
                    installedOptions.Remove(remoteOption.id);
                }
                else
                {
                    options.Add(remoteOption);
                }
            }

            options.AddRange(installedOptions.Values);
            return options;
        }

        private static StageAvatarOption[] BuildRemoteCatalogOptions(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return Array.Empty<StageAvatarOption>();
            }

            return new[]
            {
                new StageAvatarOption
                {
                    id = "downloadable:avatar-sample-a-glb-v1",
                    kind = StageAvatarOptionKind.DownloadableGlb,
                    title = "Avatar Sample A",
                    subtitle = "Prepared GLB avatar package served from GCS.",
                    runtimeAssetRemoteUrl = RemoteUrl(baseUrl, "avatars/avatar-sample-a/1.0.0/model.glb"),
                    runtimeAssetSizeBytes = 26781812,
                    sourceFilename = "avatar-sample-a.glb",
                },
                new StageAvatarOption
                {
                    id = "downloadable:avatar-sample-b-glb-v1",
                    kind = StageAvatarOptionKind.DownloadableGlb,
                    title = "Avatar Sample B",
                    subtitle = "Second prepared GLB avatar package served from GCS.",
                    runtimeAssetRemoteUrl = RemoteUrl(baseUrl, "avatars/avatar-sample-b/1.0.0/model.glb"),
                    runtimeAssetSizeBytes = 28333772,
                    sourceFilename = "avatar-sample-b.glb",
                },
            };
        }

        public StageAvatarOption InstallDevelopmentAvatar(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Avatar file was not found.", sourcePath);
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension != ".glb")
            {
                throw new InvalidOperationException($"Unsupported avatar file type: {extension}");
            }

            Directory.CreateDirectory(baseDirectoryPath);

            var displayName = DisplayName(Path.GetFileNameWithoutExtension(sourcePath));
            var slug = Slug(displayName);
            if (string.IsNullOrEmpty(slug))
            {
                slug = "avatar";
            }

            var avatarId = $"{slug}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var avatarDirectoryPath = Path.Combine(baseDirectoryPath, avatarId);
            Directory.CreateDirectory(avatarDirectoryPath);

            var runtimeAssetFilename = "model.glb";
            var runtimeAssetPath = Path.Combine(avatarDirectoryPath, runtimeAssetFilename);
            File.Copy(sourcePath, runtimeAssetPath, true);

            var sourceFileInfo = new FileInfo(sourcePath);
            var manifest = new AvatarPackageManifest
            {
                schemaVersion = 1,
                avatarId = avatarId,
                displayName = displayName,
                sourceFilename = Path.GetFileName(sourcePath),
                runtimeAssetFilename = runtimeAssetFilename,
                sourceFileByteCount = sourceFileInfo.Length,
                installedAtUtc = DateTime.UtcNow.ToString("O"),
            };
            File.WriteAllText(Path.Combine(avatarDirectoryPath, "package_manifest.json"), JsonUtility.ToJson(manifest, true));

            return OptionFromManifest(manifest, runtimeAssetPath);
        }

        public async Task<StageAvatarOption> InstallDownloadableAvatarAsync(StageAvatarOption option)
        {
            if (option == null || option.kind != StageAvatarOptionKind.DownloadableGlb)
            {
                throw new InvalidOperationException("Avatar option is not downloadable.");
            }

            if (string.IsNullOrEmpty(option.runtimeAssetRemoteUrl))
            {
                throw new InvalidOperationException("Avatar package is missing a remote GLB URL.");
            }

            Directory.CreateDirectory(baseDirectoryPath);

            var avatarId = option.id.Replace("downloadable:", string.Empty);
            var avatarDirectoryPath = Path.Combine(baseDirectoryPath, avatarId);
            Directory.CreateDirectory(avatarDirectoryPath);

            var runtimeAssetFilename = "model.glb";
            var runtimeAssetPath = Path.Combine(avatarDirectoryPath, runtimeAssetFilename);
            var temporaryPath = runtimeAssetPath + ".download";
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            using (var request = UnityWebRequest.Get(option.runtimeAssetRemoteUrl))
            {
                request.downloadHandler = new DownloadHandlerFile(temporaryPath);
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(request.error);
                }
            }

            if (File.Exists(runtimeAssetPath))
            {
                File.Delete(runtimeAssetPath);
            }

            File.Move(temporaryPath, runtimeAssetPath);

            var manifest = new AvatarPackageManifest
            {
                schemaVersion = 1,
                avatarId = avatarId,
                displayName = option.title,
                sourceFilename = option.sourceFilename,
                runtimeAssetFilename = runtimeAssetFilename,
                sourceFileByteCount = new FileInfo(runtimeAssetPath).Length,
                installedAtUtc = DateTime.UtcNow.ToString("O"),
            };
            File.WriteAllText(Path.Combine(avatarDirectoryPath, "package_manifest.json"), JsonUtility.ToJson(manifest, true));

            return OptionFromManifest(manifest, runtimeAssetPath, option.id, StageAvatarOptionKind.DownloadableGlb);
        }

        private IEnumerable<StageAvatarOption> FetchInstalledDevelopmentAvatars()
        {
            if (!Directory.Exists(baseDirectoryPath))
            {
                yield break;
            }

            var directories = Directory.GetDirectories(baseDirectoryPath);
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
            for (var directoryIndex = 0; directoryIndex < directories.Length; directoryIndex += 1)
            {
                var directory = directories[directoryIndex];
                var manifestPath = Path.Combine(directory, "package_manifest.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                StageAvatarOption option = null;
                try
                {
                    var manifest = JsonUtility.FromJson<AvatarPackageManifest>(File.ReadAllText(manifestPath));
                    var runtimeAssetPath = Path.Combine(directory, manifest.runtimeAssetFilename);
                    if (File.Exists(runtimeAssetPath))
                    {
                        var directoryName = Path.GetFileName(directory);
                        var remoteId = $"downloadable:{directoryName}";
                        var kind = IsKnownRemoteOption(remoteId)
                            ? StageAvatarOptionKind.DownloadableGlb
                            : StageAvatarOptionKind.LocalDevelopmentGlb;
                        option = OptionFromManifest(manifest, runtimeAssetPath, kind == StageAvatarOptionKind.DownloadableGlb ? remoteId : null, kind);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to load avatar package manifest at {manifestPath}: {exception.Message}");
                }

                if (option != null)
                {
                    yield return option;
                }
            }
        }

        private static void AddResourceOption(List<StageAvatarOption> options, string resourcePath, string fallbackTitle)
        {
            if (!HumanoidAvatarView.HasResource(resourcePath))
            {
                return;
            }

            options.Add(new StageAvatarOption
            {
                id = $"resources:{resourcePath}",
                kind = StageAvatarOptionKind.ResourcesPrefab,
                title = fallbackTitle,
                subtitle = $"Bundled humanoid prefab at Resources/{resourcePath}.",
                resourcePath = resourcePath,
                isInstalled = true,
            });
        }

        private StageAvatarOption OptionFromManifest(
            AvatarPackageManifest manifest,
            string runtimeAssetPath,
            string optionId = null,
            StageAvatarOptionKind kind = StageAvatarOptionKind.LocalDevelopmentGlb
        )
        {
            return new StageAvatarOption
            {
                id = string.IsNullOrEmpty(optionId) ? $"local-dev:{manifest.avatarId}" : optionId,
                kind = kind,
                title = string.IsNullOrWhiteSpace(manifest.displayName) ? "Imported GLB" : manifest.displayName,
                subtitle = kind == StageAvatarOptionKind.DownloadableGlb
                    ? "Downloaded and ready for stage playback."
                    : string.IsNullOrWhiteSpace(manifest.sourceFilename)
                        ? "Imported local GLB."
                        : $"Imported from {manifest.sourceFilename}.",
                runtimeAssetPath = runtimeAssetPath,
                sourceFilename = manifest.sourceFilename,
                isInstalled = true,
            };
        }

        private bool IsKnownRemoteOption(string optionId)
        {
            for (var optionIndex = 0; optionIndex < remoteCatalogOptions.Length; optionIndex += 1)
            {
                if (remoteCatalogOptions[optionIndex].id == optionId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string LoadConfiguredRemoteAvatarStorageBaseUrl()
        {
#if UNITY_EDITOR
            var environmentValue = Environment.GetEnvironmentVariable("ODORO_AVATAR_STORAGE_BASE_URL");
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }
#endif

            var configAsset = Resources.Load<TextAsset>(AvatarStorageConfigResourcePath);
            if (configAsset == null || string.IsNullOrWhiteSpace(configAsset.text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<AvatarStorageConfig>(configAsset.text)?.remoteAvatarStorageBaseUrl;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read avatar storage config: {exception.Message}");
                return null;
            }
        }

        private static string NormalizeBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || string.IsNullOrEmpty(uri.Scheme)
                || string.IsNullOrEmpty(uri.Host))
            {
                Debug.LogWarning("Avatar storage base URL is invalid.");
                return null;
            }

            return trimmed.TrimEnd('/');
        }

        private static string RemoteUrl(string baseUrl, string relativePath)
        {
            return $"{baseUrl}/{relativePath.TrimStart('/')}";
        }

        private static string DisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Avatar";
            }

            var parts = rawName.Replace('_', ' ').Replace('-', ' ').Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );
            for (var partIndex = 0; partIndex < parts.Length; partIndex += 1)
            {
                parts[partIndex] = char.ToUpperInvariant(parts[partIndex][0])
                    + parts[partIndex].Substring(1).ToLowerInvariant();
            }

            return parts.Length == 0 ? "Avatar" : string.Join(" ", parts);
        }

        private static string Slug(string rawName)
        {
            var chars = rawName.ToLowerInvariant().ToCharArray();
            for (var index = 0; index < chars.Length; index += 1)
            {
                if (!char.IsLetterOrDigit(chars[index]))
                {
                    chars[index] = '-';
                }
            }

            return new string(chars).Trim('-');
        }
    }
}
