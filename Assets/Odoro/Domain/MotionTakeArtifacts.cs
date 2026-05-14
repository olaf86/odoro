using UnityEngine;

namespace Odoro
{
    public sealed class MotionTakeArtifacts
    {
        public MotionClip playbackClip;
    }

    public static class MotionTakeArtifactsBuilder
    {
        public static MotionTakeArtifacts Build(MotionClip sourceClip, CaptureMode captureMode)
        {
            return new MotionTakeArtifacts
            {
                playbackClip = MotionPlaybackClipPreparer.Prepare(sourceClip, captureMode),
            };
        }
    }

    public static class MotionPlaybackClipPreparer
    {
        public static MotionClip Prepare(MotionClip sourceClip, CaptureMode captureMode)
        {
            if (sourceClip == null || sourceClip.IsEmpty)
            {
                return sourceClip;
            }

            var canonicalClip = captureMode == CaptureMode.Mock
                ? sourceClip
                : OdoroCanonicalPoseMapper.CanonicalizedClip(sourceClip);
            return MotionClipStageRebaser.Rebased(canonicalClip);
        }
    }

    public static class MotionClipStageRebaser
    {
        public static MotionClip Rebased(MotionClip clip)
        {
            if (clip == null || clip.IsEmpty)
            {
                return clip;
            }

            var originFrame = clip.frames[0];
            var average = AveragePosition(originFrame.jointPositions);
            var trackingCenter = PlaybackTrackingCenter(originFrame) ?? average;
            var floorHeight = EstimateFloorHeight(clip);
            var origin = new Vector3(trackingCenter.x, floorHeight, trackingCenter.z);

            var rebasedClip = new MotionClip();
            for (var frameIndex = 0; frameIndex < clip.frames.Count; frameIndex += 1)
            {
                var sourceFrame = clip.frames[frameIndex];
                var rebasedPositions = new Vector3[sourceFrame.jointPositions.Length];
                for (var jointIndex = 0; jointIndex < rebasedPositions.Length; jointIndex += 1)
                {
                    rebasedPositions[jointIndex] = sourceFrame.jointPositions[jointIndex] - origin;
                }

                rebasedClip.frames.Add(new MotionFrame
                {
                    time = sourceFrame.time,
                    jointPositions = rebasedPositions,
                    jointRotations = sourceFrame.jointRotations == null
                        ? null
                        : (MotionJointRotation[])sourceFrame.jointRotations.Clone(),
                });
            }

            return rebasedClip;
        }

        private static Vector3? PlaybackTrackingCenter(MotionFrame frame)
        {
            if (frame.jointPositions == null || frame.jointPositions.Length == 0)
            {
                return null;
            }

            if (frame.jointPositions.Length >= OdoroSkeletonDefinition.JointCount)
            {
                var rootIndex = OdoroSkeletonDefinition.IndexOf(OdoroJointName.Root);
                if (rootIndex >= 0 && rootIndex < frame.jointPositions.Length)
                {
                    var root = frame.jointPositions[rootIndex];
                    if (IsValidStagePosition(root))
                    {
                        return root;
                    }
                }
            }

            return AveragePosition(frame.jointPositions);
        }

        private static float EstimateFloorHeight(MotionClip clip)
        {
            var floorHeight = float.PositiveInfinity;
            for (var frameIndex = 0; frameIndex < clip.frames.Count; frameIndex += 1)
            {
                var positions = clip.frames[frameIndex].jointPositions;
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    var position = positions[jointIndex];
                    if (!IsValidStagePosition(position))
                    {
                        continue;
                    }

                    floorHeight = Mathf.Min(floorHeight, position.y);
                }
            }

            return float.IsPositiveInfinity(floorHeight) ? 0f : floorHeight;
        }

        private static Vector3 AveragePosition(Vector3[] positions)
        {
            if (positions == null || positions.Length == 0)
            {
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            var count = 0;
            for (var i = 0; i < positions.Length; i += 1)
            {
                if (!IsValidStagePosition(positions[i]))
                {
                    continue;
                }

                sum += positions[i];
                count += 1;
            }

            return count == 0 ? Vector3.zero : sum / count;
        }

        private static bool IsValidStagePosition(Vector3 position)
        {
            return float.IsFinite(position.x)
                && float.IsFinite(position.y)
                && float.IsFinite(position.z)
                && position.y > -5f;
        }
    }
}
