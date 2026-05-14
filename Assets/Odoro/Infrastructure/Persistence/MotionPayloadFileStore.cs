using System.IO;
using UnityEngine;

namespace Odoro
{
    public sealed class MotionPayloadFileStore
    {
        private readonly string baseDirectoryPath;

        public MotionPayloadFileStore(string baseDirectoryPath = null)
        {
            this.baseDirectoryPath = string.IsNullOrEmpty(baseDirectoryPath)
                ? Path.Combine(Application.persistentDataPath, "OdoroArchiveV2", "Clips")
                : baseDirectoryPath;
        }

        public string PayloadPathFor(string takeId)
        {
            return Path.Combine(baseDirectoryPath, $"{takeId}.odoro.stage");
        }

        public string Write(MotionPayload payload, string takeId)
        {
            EnsureDirectoryExists();
            var path = PayloadPathFor(takeId);
            File.WriteAllText(path, JsonUtility.ToJson(payload, true));
            return path;
        }

        public MotionPayload Read(string path)
        {
            return JsonUtility.FromJson<MotionPayload>(File.ReadAllText(path));
        }

        public void Remove(string takeId)
        {
            var path = PayloadPathFor(takeId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(baseDirectoryPath))
            {
                Directory.CreateDirectory(baseDirectoryPath);
            }
        }
    }
}
