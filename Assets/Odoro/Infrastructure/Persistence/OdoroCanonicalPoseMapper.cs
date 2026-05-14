using UnityEngine;

namespace Odoro
{
    public static class OdoroCanonicalPoseMapper
    {
        public static MotionClip CanonicalizedClip(MotionClip clip)
        {
            if (clip == null || clip.IsEmpty)
            {
                return clip;
            }

            if (clip.frames[0].jointPositions.Length == OdoroSkeletonDefinition.JointCount)
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

        private static MotionFrame CanonicalizedFrame(MotionFrame frame)
        {
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
    }
}
