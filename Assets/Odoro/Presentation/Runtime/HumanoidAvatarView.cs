using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class HumanoidAvatarView
    {
        private sealed class BoneBinding
        {
            public Transform bone;
            public OdoroJointName startJoint;
            public OdoroJointName endJoint;
            public Quaternion restRotation;
            public Vector3 restDirection;
        }

        private readonly GameObject root;
        private readonly Animator animator;
        private readonly List<BoneBinding> bindings = new List<BoneBinding>();
        private readonly Vector3 rootToHipsOffset;

        private HumanoidAvatarView(GameObject root, Animator animator, List<BoneBinding> bindings, Vector3 rootToHipsOffset)
        {
            this.root = root;
            this.animator = animator;
            this.bindings = bindings;
            this.rootToHipsOffset = rootToHipsOffset;
        }

        public bool IsAvailable => root != null && animator != null;

        public static HumanoidAvatarView TryCreateFromResources(string resourcePath = "Odoro/DefaultAvatar")
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "Odoro Stage Avatar";
            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.isHuman)
            {
                Debug.LogWarning(
                    "Default avatar prefab must contain a humanoid Animator to be driven by Odoro."
                );
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            animator.enabled = false;
            var bindings = BuildBindings(animator);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var rootToHipsOffset = hips == null
                ? Vector3.zero
                : hips.position - instance.transform.position;

            instance.SetActive(false);
            return new HumanoidAvatarView(instance, animator, bindings, rootToHipsOffset);
        }

        public void SetVisible(bool isVisible)
        {
            if (root != null && root.activeSelf != isVisible)
            {
                root.SetActive(isVisible);
            }
        }

        public void SetFrame(MotionFrame frame)
        {
            if (!IsAvailable || frame == null || frame.jointPositions == null)
            {
                SetVisible(false);
                return;
            }

            if (frame.jointPositions.Length < OdoroSkeletonDefinition.JointCount)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            var rootPosition = frame.jointPositions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.Root)];
            root.transform.position = rootPosition - rootToHipsOffset;

            for (var bindingIndex = 0; bindingIndex < bindings.Count; bindingIndex += 1)
            {
                var binding = bindings[bindingIndex];
                if (binding.bone == null)
                {
                    continue;
                }

                var startIndex = OdoroSkeletonDefinition.IndexOf(binding.startJoint);
                var endIndex = OdoroSkeletonDefinition.IndexOf(binding.endJoint);
                if (startIndex < 0 || endIndex < 0)
                {
                    continue;
                }

                var start = frame.jointPositions[startIndex];
                var end = frame.jointPositions[endIndex];
                var desiredDirection = end - start;
                if (desiredDirection.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                binding.bone.rotation = Quaternion.FromToRotation(binding.restDirection, desiredDirection.normalized)
                    * binding.restRotation;
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static List<BoneBinding> BuildBindings(Animator animator)
        {
            var bindings = new List<BoneBinding>();

            TryAddBinding(bindings, animator, HumanBodyBones.Hips, OdoroJointName.Root, OdoroJointName.Spine);
            TryAddBinding(bindings, animator, HumanBodyBones.Spine, OdoroJointName.Spine, OdoroJointName.Chest);
            TryAddBinding(bindings, animator, HumanBodyBones.Chest, OdoroJointName.Chest, OdoroJointName.Neck);
            TryAddBinding(bindings, animator, HumanBodyBones.Neck, OdoroJointName.Neck, OdoroJointName.Head);
            TryAddBinding(bindings, animator, HumanBodyBones.Head, OdoroJointName.Neck, OdoroJointName.Head);

            TryAddBinding(bindings, animator, HumanBodyBones.LeftShoulder, OdoroJointName.Chest, OdoroJointName.LeftShoulder);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftUpperArm, OdoroJointName.LeftShoulder, OdoroJointName.LeftElbow);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftLowerArm, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftHand, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist);

            TryAddBinding(bindings, animator, HumanBodyBones.RightShoulder, OdoroJointName.Chest, OdoroJointName.RightShoulder);
            TryAddBinding(bindings, animator, HumanBodyBones.RightUpperArm, OdoroJointName.RightShoulder, OdoroJointName.RightElbow);
            TryAddBinding(bindings, animator, HumanBodyBones.RightLowerArm, OdoroJointName.RightElbow, OdoroJointName.RightWrist);
            TryAddBinding(bindings, animator, HumanBodyBones.RightHand, OdoroJointName.RightElbow, OdoroJointName.RightWrist);

            TryAddBinding(bindings, animator, HumanBodyBones.LeftUpperLeg, OdoroJointName.LeftHip, OdoroJointName.LeftKnee);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftLowerLeg, OdoroJointName.LeftKnee, OdoroJointName.LeftAnkle);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftFoot, OdoroJointName.LeftAnkle, OdoroJointName.LeftFoot);

            TryAddBinding(bindings, animator, HumanBodyBones.RightUpperLeg, OdoroJointName.RightHip, OdoroJointName.RightKnee);
            TryAddBinding(bindings, animator, HumanBodyBones.RightLowerLeg, OdoroJointName.RightKnee, OdoroJointName.RightAnkle);
            TryAddBinding(bindings, animator, HumanBodyBones.RightFoot, OdoroJointName.RightAnkle, OdoroJointName.RightFoot);

            return bindings;
        }

        private static void TryAddBinding(
            List<BoneBinding> bindings,
            Animator animator,
            HumanBodyBones humanBone,
            OdoroJointName startJoint,
            OdoroJointName endJoint
        )
        {
            var bone = animator.GetBoneTransform(humanBone);
            if (bone == null)
            {
                return;
            }

            var child = FirstChildBone(bone);
            var restDirection = child == null ? Vector3.up : (child.position - bone.position).normalized;
            if (restDirection.sqrMagnitude < 0.0001f)
            {
                restDirection = Vector3.up;
            }

            bindings.Add(new BoneBinding
            {
                bone = bone,
                startJoint = startJoint,
                endJoint = endJoint,
                restRotation = bone.rotation,
                restDirection = restDirection,
            });
        }

        private static Transform FirstChildBone(Transform transform)
        {
            if (transform == null || transform.childCount == 0)
            {
                return null;
            }

            for (var childIndex = 0; childIndex < transform.childCount; childIndex += 1)
            {
                var child = transform.GetChild(childIndex);
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    return child;
                }
            }

            return transform.GetChild(0);
        }
    }
}
