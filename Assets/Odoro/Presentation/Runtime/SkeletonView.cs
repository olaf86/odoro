using UnityEngine;

namespace Odoro
{
    public sealed class SkeletonView
    {
        private static readonly (OdoroJointName, OdoroJointName)[] Bones =
        {
            (OdoroJointName.Root, OdoroJointName.Spine),
            (OdoroJointName.Spine, OdoroJointName.Chest),
            (OdoroJointName.Chest, OdoroJointName.Neck),
            (OdoroJointName.Neck, OdoroJointName.Head),
            (OdoroJointName.Chest, OdoroJointName.LeftShoulder),
            (OdoroJointName.LeftShoulder, OdoroJointName.LeftUpperArm),
            (OdoroJointName.LeftUpperArm, OdoroJointName.LeftElbow),
            (OdoroJointName.LeftElbow, OdoroJointName.LeftWrist),
            (OdoroJointName.Chest, OdoroJointName.RightShoulder),
            (OdoroJointName.RightShoulder, OdoroJointName.RightUpperArm),
            (OdoroJointName.RightUpperArm, OdoroJointName.RightElbow),
            (OdoroJointName.RightElbow, OdoroJointName.RightWrist),
            (OdoroJointName.Root, OdoroJointName.LeftHip),
            (OdoroJointName.LeftHip, OdoroJointName.LeftKnee),
            (OdoroJointName.LeftKnee, OdoroJointName.LeftAnkle),
            (OdoroJointName.LeftAnkle, OdoroJointName.LeftFoot),
            (OdoroJointName.Root, OdoroJointName.RightHip),
            (OdoroJointName.RightHip, OdoroJointName.RightKnee),
            (OdoroJointName.RightKnee, OdoroJointName.RightAnkle),
            (OdoroJointName.RightAnkle, OdoroJointName.RightFoot),
        };

        private readonly GameObject root;
        private readonly Transform[] joints = new Transform[OdoroSkeletonDefinition.JointCount];
        private readonly LineRenderer[] bones = new LineRenderer[Bones.Length];
        private readonly Material lineMaterial;

        public SkeletonView(string rootName)
        {
            root = new GameObject(rootName);
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            lineMaterial = new Material(shader);

            for (var index = 0; index < joints.Length; index += 1)
            {
                var joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                joint.name = $"Joint {index:00}";
                joint.transform.SetParent(root.transform, false);
                joint.transform.localScale = Vector3.one * 0.05f;
                var collider = joint.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                joints[index] = joint.transform;
            }

            for (var index = 0; index < Bones.Length; index += 1)
            {
                var lineObject = new GameObject($"Bone {index:00}");
                lineObject.transform.SetParent(root.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = 0.02f;
                line.useWorldSpace = true;
                line.material = lineMaterial;
                line.numCapVertices = 8;
                bones[index] = line;
            }
        }

        public void SetFrame(MotionFrame frame, Color color)
        {
            root.SetActive(frame != null);
            if (frame == null || frame.jointPositions == null || frame.jointPositions.Length < joints.Length)
            {
                return;
            }

            for (var index = 0; index < joints.Length; index += 1)
            {
                joints[index].position = frame.jointPositions[index];
                var renderer = joints[index].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }

            for (var index = 0; index < Bones.Length; index += 1)
            {
                var start = OdoroSkeletonDefinition.IndexOf(Bones[index].Item1);
                var end = OdoroSkeletonDefinition.IndexOf(Bones[index].Item2);
                bones[index].startColor = color;
                bones[index].endColor = color;
                bones[index].SetPosition(0, frame.jointPositions[start]);
                bones[index].SetPosition(1, frame.jointPositions[end]);
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }

            if (lineMaterial != null)
            {
                Object.Destroy(lineMaterial);
            }
        }
    }
}
