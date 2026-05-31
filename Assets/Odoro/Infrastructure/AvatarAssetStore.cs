using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class AvatarAssetStore
    {
        private const string DefaultAvatarResourcePath = "Odoro/DefaultAvatar";
        private const string AvatarResourcesPath = "Odoro/Avatars";

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

            return options;
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
