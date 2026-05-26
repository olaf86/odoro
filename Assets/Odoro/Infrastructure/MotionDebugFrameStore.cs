using System;
using System.IO;
using UnityEngine;

namespace Odoro
{
    public static class MotionDebugFrameStore
    {
        public const string ReplayFileName = "editor-replay.odoro.source.json";

        private const string DebugDirectoryName = "OdoroDebug";
        private const string MotionFramesDirectoryName = "MotionFrames";

        public static string DebugDirectoryPath =>
            Path.Combine(Application.persistentDataPath, DebugDirectoryName, MotionFramesDirectoryName);

        public static string DefaultReplayPath => Path.Combine(DebugDirectoryPath, ReplayFileName);

#if UNITY_EDITOR
        public static string ProjectReplayPath =>
            Path.Combine(Application.dataPath, "Odoro", "DebugMotionReplay", ReplayFileName);
#endif

        public static string WriteReplay(
            MotionClip clip,
            CaptureMode captureMode,
            string sourcePlatform,
            string sourceBackend
        )
        {
            var takeId = $"motion-debug-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var store = new MotionSourceClipFileStore(DebugDirectoryPath);
            var path = store.Write(clip, takeId, captureMode, sourcePlatform, sourceBackend);

            Directory.CreateDirectory(DebugDirectoryPath);
            File.Copy(path, DefaultReplayPath, true);
            return path;
        }

        public static bool TryReadReplay(out MotionClip clip, out string path)
        {
            clip = null;
            path = null;

#if UNITY_EDITOR
            if (TryReadReplayPath(ProjectReplayPath, out clip))
            {
                path = ProjectReplayPath;
                return true;
            }
#endif

            if (TryReadReplayPath(DefaultReplayPath, out clip))
            {
                path = DefaultReplayPath;
                return true;
            }

            return false;
        }

        private static bool TryReadReplayPath(string path, out MotionClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                clip = new MotionSourceClipFileStore(Path.GetDirectoryName(path)).ReadFromPath(path);
                return clip != null && !clip.IsEmpty;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read motion debug replay at {path}: {exception.Message}");
                clip = null;
                return false;
            }
        }

    }
}
