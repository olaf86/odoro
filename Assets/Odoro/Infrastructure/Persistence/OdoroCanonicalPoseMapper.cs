using UnityEngine;

namespace Odoro
{
    public static class OdoroCanonicalPoseMapper
    {
        private const float MinAxisMagnitude = 0.0001f;

        public static MotionClip CanonicalizedClip(MotionClip clip)
        {
            if (clip == null || clip.IsEmpty)
            {
                return clip;
            }

            if (clip.frames[0]?.jointPositions?.Length == OdoroSkeletonDefinition.JointCount)
            {
                return clip;
            }

            var canonicalClip = new MotionClip();
            for (var i = 0; i < clip.frames.Count; i += 1)
            {
                canonicalClip.frames.Add(CanonicalizedFrame(clip.frames[i]));
            }

            return canonicalClip;
        }

        public static MotionClip AvatarSpaceClip(MotionClip clip)
        {
            var canonicalClip = CanonicalizedClip(clip);
            if (canonicalClip == null || canonicalClip.IsEmpty)
            {
                return canonicalClip;
            }

            var avatarClip = new MotionClip();
            for (var i = 0; i < canonicalClip.frames.Count; i += 1)
            {
                avatarClip.frames.Add(AvatarSpaceFrame(canonicalClip.frames[i]));
            }

            return avatarClip;
        }

        public static MotionFrame AvatarSpaceFrame(MotionFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            // Stage avatars face the viewer, so source display-space left/right needs to be mirrored
            // before any skeleton or humanoid renderer consumes the playback frame.
            var canonicalFrame = CanonicalizedFrame(frame);
            if (canonicalFrame.jointPositions == null
                || canonicalFrame.jointPositions.Length < OdoroSkeletonDefinition.JointCount
                || !TryBodySideAxis(canonicalFrame.jointPositions, out var sideAxis)
                || !TryBodyCenter(canonicalFrame.jointPositions, out var center))
            {
                return canonicalFrame;
            }

            var positions = new Vector3[canonicalFrame.jointPositions.Length];
            for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
            {
                positions[jointIndex] = ReflectAcrossBodyCenterPlane(
                    canonicalFrame.jointPositions[jointIndex],
                    center,
                    sideAxis
                );
            }

            return new MotionFrame
            {
                time = canonicalFrame.time,
                jointPositions = positions,
                jointRotations = canonicalFrame.jointRotations == null
                    ? null
                    : (MotionJointRotation[])canonicalFrame.jointRotations.Clone(),
            };
        }

        private static MotionFrame CanonicalizedFrame(MotionFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            if (frame.jointPositions == null || frame.jointPositions.Length >= OdoroSkeletonDefinition.JointCount)
            {
                return frame;
            }

            var joints = frame.jointPositions;
            var root = joints[0];
            var head = joints[1];
            var leftShoulder = joints[3];
            var rightShoulder = joints[4];
            var leftElbow = joints[5];
            var rightElbow = joints[6];
            var leftWrist = joints[7];
            var rightWrist = joints[8];
            var leftHip = joints[9];
            var rightHip = joints[10];
            var leftKnee = joints[11];
            var rightKnee = joints[12];
            var leftAnkle = joints[13];
            var rightAnkle = joints[14];
            var leftFoot = joints[15];
            var rightFoot = joints[16];

            var neck = Vector3.Lerp(root, head, 0.78f);
            var chest = Vector3.Lerp(root, neck, 0.68f);
            var spine = Vector3.Lerp(root, chest, 0.52f);
            var leftUpperArm = Vector3.Lerp(leftShoulder, leftElbow, 0.42f);
            var rightUpperArm = Vector3.Lerp(rightShoulder, rightElbow, 0.42f);

            return new MotionFrame
            {
                time = frame.time,
                jointPositions = new[]
                {
                    root,
                    spine,
                    chest,
                    neck,
                    head,
                    leftShoulder,
                    rightShoulder,
                    leftElbow,
                    rightElbow,
                    leftWrist,
                    rightWrist,
                    leftHip,
                    rightHip,
                    leftKnee,
                    rightKnee,
                    leftAnkle,
                    rightAnkle,
                    leftFoot,
                    rightFoot,
                    leftUpperArm,
                    rightUpperArm,
                },
                jointRotations = null,
            };
        }

        private static bool TryBodySideAxis(Vector3[] positions, out Vector3 sideAxis)
        {
            sideAxis = Vector3.zero;
            AddSideAxis(
                positions,
                ref sideAxis,
                OdoroJointName.LeftShoulder,
                OdoroJointName.RightShoulder,
                1f
            );
            AddSideAxis(positions, ref sideAxis, OdoroJointName.LeftHip, OdoroJointName.RightHip, 0.65f);
            AddSideAxis(
                positions,
                ref sideAxis,
                OdoroJointName.LeftUpperArm,
                OdoroJointName.RightUpperArm,
                0.35f
            );

            if (TryBodyUpAxis(positions, out var upAxis))
            {
                sideAxis = Vector3.ProjectOnPlane(sideAxis, upAxis);
            }

            if (sideAxis.sqrMagnitude < MinAxisMagnitude)
            {
                return false;
            }

            sideAxis.Normalize();
            return true;
        }

        private static void AddSideAxis(
            Vector3[] positions,
            ref Vector3 sideAxis,
            OdoroJointName leftJoint,
            OdoroJointName rightJoint,
            float weight
        )
        {
            if (!TryJointPosition(positions, leftJoint, out var left)
                || !TryJointPosition(positions, rightJoint, out var right))
            {
                return;
            }

            var delta = right - left;
            if (delta.sqrMagnitude < MinAxisMagnitude)
            {
                return;
            }

            sideAxis += delta.normalized * weight;
        }

        private static bool TryBodyUpAxis(Vector3[] positions, out Vector3 upAxis)
        {
            upAxis = Vector3.zero;
            if (TryJointPosition(positions, OdoroJointName.Root, out var root)
                && TryJointPosition(positions, OdoroJointName.Head, out var head))
            {
                upAxis = head - root;
            }

            if (upAxis.sqrMagnitude < MinAxisMagnitude
                && TryJointPosition(positions, OdoroJointName.LeftHip, out var leftHip)
                && TryJointPosition(positions, OdoroJointName.RightHip, out var rightHip)
                && TryJointPosition(positions, OdoroJointName.LeftShoulder, out var leftShoulder)
                && TryJointPosition(positions, OdoroJointName.RightShoulder, out var rightShoulder))
            {
                upAxis = Vector3.Lerp(leftShoulder, rightShoulder, 0.5f)
                    - Vector3.Lerp(leftHip, rightHip, 0.5f);
            }

            if (upAxis.sqrMagnitude < MinAxisMagnitude)
            {
                return false;
            }

            upAxis.Normalize();
            return true;
        }

        private static bool TryBodyCenter(Vector3[] positions, out Vector3 center)
        {
            center = Vector3.zero;
            var count = 0;
            AddCenterSample(positions, ref center, ref count, OdoroJointName.Root);
            AddMidpointCenterSample(positions, ref center, ref count, OdoroJointName.LeftHip, OdoroJointName.RightHip);
            AddMidpointCenterSample(
                positions,
                ref center,
                ref count,
                OdoroJointName.LeftShoulder,
                OdoroJointName.RightShoulder
            );

            if (count == 0)
            {
                return false;
            }

            center /= count;
            return true;
        }

        private static void AddCenterSample(
            Vector3[] positions,
            ref Vector3 center,
            ref int count,
            OdoroJointName jointName
        )
        {
            if (!TryJointPosition(positions, jointName, out var position))
            {
                return;
            }

            center += position;
            count += 1;
        }

        private static void AddMidpointCenterSample(
            Vector3[] positions,
            ref Vector3 center,
            ref int count,
            OdoroJointName leftJoint,
            OdoroJointName rightJoint
        )
        {
            if (!TryJointPosition(positions, leftJoint, out var left)
                || !TryJointPosition(positions, rightJoint, out var right))
            {
                return;
            }

            center += Vector3.Lerp(left, right, 0.5f);
            count += 1;
        }

        private static bool TryJointPosition(Vector3[] positions, OdoroJointName jointName, out Vector3 position)
        {
            var index = OdoroSkeletonDefinition.IndexOf(jointName);
            if (positions == null || index < 0 || index >= positions.Length || !IsFinite(positions[index]))
            {
                position = Vector3.zero;
                return false;
            }

            position = positions[index];
            return true;
        }

        private static Vector3 ReflectAcrossBodyCenterPlane(Vector3 position, Vector3 center, Vector3 sideAxis)
        {
            var offset = position - center;
            return position - 2f * Vector3.Dot(offset, sideAxis) * sideAxis;
        }

        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.x)
                && float.IsFinite(position.y)
                && float.IsFinite(position.z);
        }
    }
}
