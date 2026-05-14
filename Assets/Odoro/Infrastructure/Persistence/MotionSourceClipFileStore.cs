using System;
using System.IO;
using UnityEngine;

namespace Odoro
{
    [Serializable]
    internal sealed class MotionSourceClipFrame
    {
        public float timeSeconds;
        public MotionPayloadVector3[] positions;
        public MotionPayloadQuaternion[] rotations;
    }

    [Serializable]
    internal sealed class MotionSourceClipPayload
    {
        public int schemaVersion = 1;
        public string skeletonId;
        public int jointCount;
        public CaptureMode captureMode;
        public string sourcePlatform;
        public string sourceBackend;
        public MotionSourceClipFrame[] frames;
    }

    public sealed class MotionSourceClipFileStore
    {
        private readonly string baseDirectoryPath;

        public MotionSourceClipFileStore(string baseDirectoryPath = null)
        {
            this.baseDirectoryPath = string.IsNullOrEmpty(baseDirectoryPath)
                ? Path.Combine(Application.persistentDataPath, "OdoroArchiveV2", "SourceClips")
                : baseDirectoryPath;
        }

        public string SourceClipPathFor(string takeId)
        {
            return Path.Combine(baseDirectoryPath, $"{takeId}.odoro.source.json");
        }

        public string Write(MotionClip clip, string takeId, CaptureMode captureMode, string sourcePlatform, string sourceBackend)
        {
            EnsureDirectoryExists();
            var path = SourceClipPathFor(takeId);
            var payload = new MotionSourceClipPayload
            {
                skeletonId = ResolveSkeletonId(clip, captureMode),
                jointCount = clip?.frames.Count > 0 ? clip.frames[0].jointPositions.Length : 0,
                captureMode = captureMode,
                sourcePlatform = sourcePlatform,
                sourceBackend = sourceBackend,
                frames = BuildFrames(clip),
            };
            File.WriteAllText(path, JsonUtility.ToJson(payload, true));
            return path;
        }

        public MotionClip Read(string takeId)
        {
            var path = SourceClipPathFor(takeId);
            return ReadFromPath(path);
        }

        public MotionClip ReadFromPath(string path)
        {
            var payload = JsonUtility.FromJson<MotionSourceClipPayload>(File.ReadAllText(path));
            var clip = new MotionClip();
            if (payload?.frames == null)
            {
                return clip;
            }

            for (var frameIndex = 0; frameIndex < payload.frames.Length; frameIndex += 1)
            {
                var sourceFrame = payload.frames[frameIndex];
                var positions = new Vector3[sourceFrame.positions.Length];
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    positions[jointIndex] = sourceFrame.positions[jointIndex].VectorValue;
                }

                MotionJointRotation[] rotations = null;
                if (sourceFrame.rotations != null)
                {
                    rotations = new MotionJointRotation[sourceFrame.rotations.Length];
                    for (var jointIndex = 0; jointIndex < rotations.Length; jointIndex += 1)
                    {
                        rotations[jointIndex] = sourceFrame.rotations[jointIndex].MotionValue;
                    }
                }

                clip.frames.Add(new MotionFrame
                {
                    time = sourceFrame.timeSeconds,
                    jointPositions = positions,
                    jointRotations = rotations,
                });
            }

            return clip;
        }

        public bool Exists(string takeId)
        {
            return File.Exists(SourceClipPathFor(takeId));
        }

        public void Remove(string takeId)
        {
            var path = SourceClipPathFor(takeId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static MotionSourceClipFrame[] BuildFrames(MotionClip clip)
        {
            if (clip == null || clip.IsEmpty)
            {
                return Array.Empty<MotionSourceClipFrame>();
            }

            var frames = new MotionSourceClipFrame[clip.frames.Count];
            for (var frameIndex = 0; frameIndex < clip.frames.Count; frameIndex += 1)
            {
                var clipFrame = clip.frames[frameIndex];
                var positions = new MotionPayloadVector3[clipFrame.jointPositions.Length];
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    positions[jointIndex] = new MotionPayloadVector3(clipFrame.jointPositions[jointIndex]);
                }

                MotionPayloadQuaternion[] rotations = null;
                if (clipFrame.jointRotations != null)
                {
                    rotations = new MotionPayloadQuaternion[clipFrame.jointRotations.Length];
                    for (var jointIndex = 0; jointIndex < rotations.Length; jointIndex += 1)
                    {
                        rotations[jointIndex] = new MotionPayloadQuaternion(clipFrame.jointRotations[jointIndex]);
                    }
                }

                frames[frameIndex] = new MotionSourceClipFrame
                {
                    timeSeconds = clipFrame.time,
                    positions = positions,
                    rotations = rotations,
                };
            }

            return frames;
        }

        private static string ResolveSkeletonId(MotionClip clip, CaptureMode captureMode)
        {
            if (captureMode == CaptureMode.RearBody3D)
            {
                return "arkit.body3d";
            }

            var jointCount = clip?.frames.Count > 0 ? clip.frames[0].jointPositions.Length : 0;
            return jointCount == OdoroSkeletonDefinition.JointCount
                ? OdoroSkeletonDefinition.Id
                : $"source.{captureMode}";
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
