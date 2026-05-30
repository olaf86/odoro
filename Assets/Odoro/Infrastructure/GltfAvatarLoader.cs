using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace Odoro
{
    public static class GltfAvatarLoader
    {
        public static async Task<HumanoidAvatarView> LoadAsync(string path, string displayName)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("Avatar GLB file was not found.", path);
            }

            var import = new GltfImport();
            var url = new Uri(path).AbsoluteUri;
            var loaded = await import.Load(url);
            if (!loaded)
            {
                throw new InvalidOperationException($"Failed to load GLB avatar: {path}");
            }

            var root = new GameObject($"Odoro Imported Avatar - {displayName}");
            var instantiated = await import.InstantiateMainSceneAsync(root.transform);
            if (!instantiated)
            {
                UnityEngine.Object.Destroy(root);
                throw new InvalidOperationException($"Failed to instantiate GLB avatar: {path}");
            }

            var view = HumanoidAvatarView.TryCreateFromInstance(root, displayName);
            if (view == null)
            {
                throw new InvalidOperationException("The GLB loaded, but no humanoid rig or recognizable bone names were found.");
            }

            return view;
        }
    }
}
