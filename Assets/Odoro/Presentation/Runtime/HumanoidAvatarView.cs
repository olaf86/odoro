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
            public Transform child;
            public OdoroJointName startJoint;
            public OdoroJointName endJoint;
            public Quaternion restLocalRotation;
            public Vector3 restLocalDirection;
        }

        private readonly GameObject root;
        private readonly List<BoneBinding> bindings = new List<BoneBinding>();
        private readonly Vector3 rootToHipsOffset;
        private readonly string displayName;
        private string[] debugLines = Array.Empty<string>();

        private HumanoidAvatarView(
            GameObject root,
            List<BoneBinding> bindings,
            Vector3 rootToHipsOffset,
            string displayName
        )
        {
            this.root = root;
            this.bindings = bindings;
            this.rootToHipsOffset = rootToHipsOffset;
            this.displayName = displayName;
        }

        public bool IsAvailable => root != null && bindings.Count > 0;

        public string DisplayName => displayName;

        public string[] DebugLines => debugLines;

        public static bool HasResource(string resourcePath)
        {
            return Resources.Load<GameObject>(resourcePath) != null;
        }

        public static HumanoidAvatarView TryCreateFromResources(string resourcePath = "Odoro/DefaultAvatar")
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "Odoro Stage Avatar";
            return TryCreateFromHumanoidInstance(instance, prefab.name);
        }

        private static HumanoidAvatarView TryCreateFromHumanoidInstance(GameObject instance, string sourceName)
        {
            if (instance == null)
            {
                return null;
            }

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.isHuman)
            {
                Debug.LogWarning($"Avatar '{sourceName}' must be imported as a Unity Humanoid Avatar.");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            AlignHumanoidFrontToStage(instance.transform, animator);
            var bindings = BuildHumanoidBindings(animator);
            if (bindings.Count == 0)
            {
                Debug.LogWarning($"Avatar '{sourceName}' does not contain a usable Unity Humanoid bone mapping.");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            animator.enabled = false;

            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var rootToHipsOffset = hips == null
                ? Vector3.zero
                : hips.position - instance.transform.position;

            instance.SetActive(false);
            return new HumanoidAvatarView(
                instance,
                bindings,
                rootToHipsOffset,
                string.IsNullOrWhiteSpace(sourceName) ? "Avatar" : sourceName
            );
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
                debugLines = Array.Empty<string>();
                return;
            }

            if (frame.jointPositions.Length < OdoroSkeletonDefinition.JointCount)
            {
                SetVisible(false);
                debugLines = Array.Empty<string>();
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

                var parent = binding.bone.parent;
                var desiredLocalDirection = parent == null
                    ? desiredDirection.normalized
                    : parent.InverseTransformDirection(desiredDirection.normalized);
                if (desiredLocalDirection.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                binding.bone.localRotation =
                    Quaternion.FromToRotation(binding.restLocalDirection, desiredLocalDirection.normalized)
                    * binding.restLocalRotation;
            }

            debugLines = BuildDebugLines(frame);
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static List<BoneBinding> BuildHumanoidBindings(Animator animator)
        {
            var bindings = new List<BoneBinding>();

            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                OdoroJointName.Root,
                OdoroJointName.Spine
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                OdoroJointName.Spine,
                OdoroJointName.Chest
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.Chest,
                HumanBodyBones.Neck,
                OdoroJointName.Chest,
                OdoroJointName.Neck
            );

            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm,
                OdoroJointName.LeftUpperArm,
                OdoroJointName.LeftElbow
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand,
                OdoroJointName.LeftElbow,
                OdoroJointName.LeftWrist
            );

            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm,
                OdoroJointName.RightUpperArm,
                OdoroJointName.RightElbow
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand,
                OdoroJointName.RightElbow,
                OdoroJointName.RightWrist
            );

            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                OdoroJointName.LeftHip,
                OdoroJointName.LeftKnee
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                OdoroJointName.LeftKnee,
                OdoroJointName.LeftAnkle
            );

            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg,
                OdoroJointName.RightHip,
                OdoroJointName.RightKnee
            );
            TryAddBinding(
                bindings,
                animator,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot,
                OdoroJointName.RightKnee,
                OdoroJointName.RightAnkle
            );

            return bindings;
        }

        private static void TryAddBinding(
            List<BoneBinding> bindings,
            Animator animator,
            HumanBodyBones humanBone,
            HumanBodyBones childHumanBone,
            OdoroJointName startJoint,
            OdoroJointName endJoint
        )
        {
            var bone = animator.GetBoneTransform(humanBone);
            if (bone == null)
            {
                return;
            }

            AddBinding(bindings, bone, startJoint, endJoint, animator.GetBoneTransform(childHumanBone));
        }

        private static void AddBinding(
            List<BoneBinding> bindings,
            Transform bone,
            OdoroJointName startJoint,
            OdoroJointName endJoint,
            Transform child = null
        )
        {
            if (child == null)
            {
                child = FirstChildBone(bone);
            }

            var restWorldDirection = child == null ? bone.TransformDirection(Vector3.up) : (child.position - bone.position).normalized;
            if (restWorldDirection.sqrMagnitude < 0.0001f)
            {
                restWorldDirection = bone.TransformDirection(Vector3.up);
            }

            var parent = bone.parent;
            var restLocalDirection = parent == null
                ? restWorldDirection.normalized
                : parent.InverseTransformDirection(restWorldDirection.normalized);
            if (restLocalDirection.sqrMagnitude < 0.0001f)
            {
                restLocalDirection = Vector3.up;
            }

            bindings.Add(new BoneBinding
            {
                bone = bone,
                child = child,
                startJoint = startJoint,
                endJoint = endJoint,
                restLocalRotation = bone.localRotation,
                restLocalDirection = restLocalDirection.normalized,
            });
        }

        private static void AlignHumanoidFrontToStage(Transform root, Animator animator)
        {
            var currentForward = EstimateHumanoidForward(animator);
            currentForward.y = 0f;
            if (currentForward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetForward = Vector3.back;
            var correction = Quaternion.FromToRotation(currentForward.normalized, targetForward);
            root.rotation = correction * root.rotation;
        }

        private static Vector3 EstimateHumanoidForward(Animator animator)
        {
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)
                ?? animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            var rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm)
                ?? animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            if (hips == null || head == null || leftShoulder == null || rightShoulder == null)
            {
                return animator.transform.forward;
            }

            var right = rightShoulder.position - leftShoulder.position;
            var up = head.position - hips.position;
            if (right.sqrMagnitude < 0.0001f || up.sqrMagnitude < 0.0001f)
            {
                return animator.transform.forward;
            }

            return Vector3.Cross(right.normalized, up.normalized);
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

        private string[] BuildDebugLines(MotionFrame frame)
        {
            var lines = new List<string>
            {
                $"Avatar debug: {displayName} ({bindings.Count} bindings)",
            };

            AddBindingDebugLine(lines, frame, "Av L shoulder", OdoroJointName.LeftShoulder, OdoroJointName.LeftUpperArm);
            AddBindingDebugLine(lines, frame, "Av L upper", OdoroJointName.LeftUpperArm, OdoroJointName.LeftElbow);
            AddBindingDebugLine(lines, frame, "Av L lower", OdoroJointName.LeftElbow, OdoroJointName.LeftWrist);
            AddBindingDebugLine(lines, frame, "Av R shoulder", OdoroJointName.RightShoulder, OdoroJointName.RightUpperArm);
            AddBindingDebugLine(lines, frame, "Av R upper", OdoroJointName.RightUpperArm, OdoroJointName.RightElbow);
            AddBindingDebugLine(lines, frame, "Av R lower", OdoroJointName.RightElbow, OdoroJointName.RightWrist);
            return lines.ToArray();
        }

        private void AddBindingDebugLine(
            List<string> lines,
            MotionFrame frame,
            string label,
            OdoroJointName startJoint,
            OdoroJointName endJoint
        )
        {
            var binding = FindBinding(startJoint, endJoint);
            if (binding == null)
            {
                lines.Add($"{label}: missing");
                return;
            }

            var actualDirection = ActualWorldDirection(binding);
            if (actualDirection.sqrMagnitude < 0.0001f)
            {
                lines.Add($"{label}: no actual dir");
                return;
            }

            var targetDirection = TargetWorldDirection(frame, startJoint, endJoint);
            if (targetDirection.sqrMagnitude < 0.0001f)
            {
                lines.Add($"{label}: no target dir");
                return;
            }

            var actual = actualDirection.normalized;
            var target = targetDirection.normalized;
            var dot = Vector3.Dot(actual, target);
            lines.Add($"{label}: dot {dot:0.00} act {VectorLabel(actual)} tgt {VectorLabel(target)}");
        }

        private BoneBinding FindBinding(OdoroJointName startJoint, OdoroJointName endJoint)
        {
            for (var bindingIndex = 0; bindingIndex < bindings.Count; bindingIndex += 1)
            {
                var binding = bindings[bindingIndex];
                if (binding.startJoint == startJoint && binding.endJoint == endJoint)
                {
                    return binding;
                }
            }

            return null;
        }

        private static Vector3 ActualWorldDirection(BoneBinding binding)
        {
            if (binding.child != null)
            {
                return binding.child.position - binding.bone.position;
            }

            var parent = binding.bone.parent;
            return parent == null
                ? binding.bone.TransformDirection(binding.restLocalDirection)
                : parent.TransformDirection(binding.restLocalDirection);
        }

        private static Vector3 TargetWorldDirection(MotionFrame frame, OdoroJointName startJoint, OdoroJointName endJoint)
        {
            var startIndex = OdoroSkeletonDefinition.IndexOf(startJoint);
            var endIndex = OdoroSkeletonDefinition.IndexOf(endJoint);
            if (frame.jointPositions == null
                || startIndex < 0
                || endIndex < 0
                || startIndex >= frame.jointPositions.Length
                || endIndex >= frame.jointPositions.Length)
            {
                return Vector3.zero;
            }

            return frame.jointPositions[endIndex] - frame.jointPositions[startIndex];
        }

        private static string VectorLabel(Vector3 vector)
        {
            return $"({vector.x:0.00},{vector.y:0.00},{vector.z:0.00})";
        }

    }
}
