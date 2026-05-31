using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class AvatarAssetStore
    {
        private const char UninstalledAvatarSeparator = '\n';
        private const string UninstalledAvatarPrefsKey = "Odoro.UninstalledAvatarOptions";
        private const string DefaultAvatarResourcePath = "Odoro/DefaultAvatar";
        private const string AvatarResourcesPath = "Odoro/Avatars";

        public IReadOnlyList<StageAvatarOption> FetchAvailableOptions()
        {
            var uninstalledAvatarIds = LoadUninstalledAvatarIds();
            var options = new List<StageAvatarOption>
            {
                new StageAvatarOption
                {
                    id = "procedural-skeleton",
                    kind = StageAvatarOptionKind.ProceduralSkeleton,
                    title = "Skeleton Preview",
                    subtitle = "Use the lightweight joint preview for motion checking.",
                    isInstalled = true,
                },
            };

            AddResourceOption(options, uninstalledAvatarIds, DefaultAvatarResourcePath, "Default Avatar", false);
            var prefabs = Resources.LoadAll<GameObject>(AvatarResourcesPath);
            Array.Sort(prefabs, (left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            for (var prefabIndex = 0; prefabIndex < prefabs.Length; prefabIndex += 1)
            {
                var prefab = prefabs[prefabIndex];
                AddResourceOption(options, uninstalledAvatarIds, $"{AvatarResourcesPath}/{prefab.name}", DisplayName(prefab.name), true);
            }

            return options;
        }

        public bool InstallAvatar(string optionId)
        {
            if (string.IsNullOrEmpty(optionId))
            {
                return false;
            }

            var uninstalledAvatarIds = LoadUninstalledAvatarIds();
            if (!uninstalledAvatarIds.Remove(optionId))
            {
                return false;
            }

            SaveUninstalledAvatarIds(uninstalledAvatarIds);
            return true;
        }

        public bool UninstallAvatar(string optionId)
        {
            if (string.IsNullOrEmpty(optionId))
            {
                return false;
            }

            var uninstalledAvatarIds = LoadUninstalledAvatarIds();
            if (!uninstalledAvatarIds.Add(optionId))
            {
                return false;
            }

            SaveUninstalledAvatarIds(uninstalledAvatarIds);
            return true;
        }

        private static void AddResourceOption(
            List<StageAvatarOption> options,
            HashSet<string> uninstalledAvatarIds,
            string resourcePath,
            string fallbackTitle,
            bool canUninstall
        )
        {
            if (!HumanoidAvatarView.HasResource(resourcePath))
            {
                return;
            }

            var optionId = $"resources:{resourcePath}";
            var isInstalled = !uninstalledAvatarIds.Contains(optionId);
            options.Add(new StageAvatarOption
            {
                id = optionId,
                kind = StageAvatarOptionKind.ResourcesPrefab,
                title = fallbackTitle,
                subtitle = isInstalled
                    ? $"Bundled humanoid prefab at Resources/{resourcePath}."
                    : "Uninstalled. Reinstall to use this avatar again.",
                resourcePath = resourcePath,
                isInstalled = isInstalled,
                canInstall = canUninstall && !isInstalled,
                canUninstall = canUninstall && isInstalled,
            });
        }

        private static HashSet<string> LoadUninstalledAvatarIds()
        {
            var ids = new HashSet<string>();
            var value = PlayerPrefs.GetString(UninstalledAvatarPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(value))
            {
                return ids;
            }

            var parts = value.Split(new[] { UninstalledAvatarSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (var partIndex = 0; partIndex < parts.Length; partIndex += 1)
            {
                ids.Add(parts[partIndex]);
            }

            return ids;
        }

        private static void SaveUninstalledAvatarIds(HashSet<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                PlayerPrefs.DeleteKey(UninstalledAvatarPrefsKey);
                PlayerPrefs.Save();
                return;
            }

            var sortedIds = new List<string>(ids);
            sortedIds.Sort(StringComparer.Ordinal);
            PlayerPrefs.SetString(UninstalledAvatarPrefsKey, string.Join(UninstalledAvatarSeparator.ToString(), sortedIds));
            PlayerPrefs.Save();
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
    }
}
