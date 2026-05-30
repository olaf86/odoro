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
            public bool mirrorArmTargetJoints;
        }

        private readonly GameObject root;
        private readonly Animator animator;
        private readonly List<BoneBinding> bindings = new List<BoneBinding>();
        private readonly Vector3 rootToHipsOffset;
        private readonly string displayName;
        private string[] debugLines = Array.Empty<string>();
        private bool debugSwapArmJoints;

        private HumanoidAvatarView(
            GameObject root,
            Animator animator,
            List<BoneBinding> bindings,
            Vector3 rootToHipsOffset,
            string displayName
        )
        {
            this.root = root;
            this.animator = animator;
            this.bindings = bindings;
            this.rootToHipsOffset = rootToHipsOffset;
            this.displayName = displayName;
        }

        public bool IsAvailable => root != null && bindings.Count > 0;

        public string DisplayName => displayName;

        public int BindingCount => bindings.Count;

        public string[] DebugLines => debugLines;

        public void SetDebugSwapArmJoints(bool isEnabled)
        {
            debugSwapArmJoints = isEnabled;
        }

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
            return TryCreateFromInstance(instance, prefab.name, false);
        }

        public static HumanoidAvatarView TryCreateFromInstance(
            GameObject instance,
            string sourceName,
            bool normalizeBoundsForStage = true
        )
        {
            if (instance == null)
            {
                return null;
            }

            if (normalizeBoundsForStage)
            {
                NormalizeBoundsForStage(instance);
            }

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator != null && animator.avatar != null && animator.isHuman)
            {
                AlignHumanoidFrontToStage(instance.transform, animator);
            }

            var bindings = animator != null && animator.avatar != null && animator.isHuman
                ? BuildHumanoidBindings(animator)
                : BuildNamedTransformBindings(instance.transform);

            if (bindings.Count == 0)
            {
                Debug.LogWarning(
                    $"Avatar '{sourceName}' does not contain a supported humanoid rig or recognizable bone names."
                );
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            var hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : FindRootBone(instance.transform);
            var rootToHipsOffset = hips == null
                ? Vector3.zero
                : hips.position - instance.transform.position;

            instance.SetActive(false);
            return new HumanoidAvatarView(
                instance,
                animator,
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

                var startJoint = ResolveTargetJoint(binding, binding.startJoint);
                var endJoint = ResolveTargetJoint(binding, binding.endJoint);
                var startIndex = OdoroSkeletonDefinition.IndexOf(startJoint);
                var endIndex = OdoroSkeletonDefinition.IndexOf(endJoint);
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

            TryAddBinding(bindings, animator, HumanBodyBones.Hips, OdoroJointName.Root, OdoroJointName.Spine);
            TryAddBinding(bindings, animator, HumanBodyBones.Spine, OdoroJointName.Spine, OdoroJointName.Chest);
            TryAddBinding(bindings, animator, HumanBodyBones.Chest, OdoroJointName.Chest, OdoroJointName.Neck);

            TryAddBinding(bindings, animator, HumanBodyBones.LeftUpperArm, OdoroJointName.LeftUpperArm, OdoroJointName.LeftElbow, true);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftLowerArm, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist, true);

            TryAddBinding(bindings, animator, HumanBodyBones.RightUpperArm, OdoroJointName.RightUpperArm, OdoroJointName.RightElbow, true);
            TryAddBinding(bindings, animator, HumanBodyBones.RightLowerArm, OdoroJointName.RightElbow, OdoroJointName.RightWrist, true);

            TryAddBinding(bindings, animator, HumanBodyBones.LeftUpperLeg, OdoroJointName.LeftHip, OdoroJointName.LeftKnee);
            TryAddBinding(bindings, animator, HumanBodyBones.LeftLowerLeg, OdoroJointName.LeftKnee, OdoroJointName.LeftAnkle);

            TryAddBinding(bindings, animator, HumanBodyBones.RightUpperLeg, OdoroJointName.RightHip, OdoroJointName.RightKnee);
            TryAddBinding(bindings, animator, HumanBodyBones.RightLowerLeg, OdoroJointName.RightKnee, OdoroJointName.RightAnkle);

            return bindings;
        }

        private static List<BoneBinding> BuildNamedTransformBindings(Transform root)
        {
            var bones = new Dictionary<string, Transform>();
            CollectBones(root, bones);

            var bindings = new List<BoneBinding>();
            TryAddNamedBinding(bindings, bones, OdoroJointName.Root, OdoroJointName.Spine, "hips", "pelvis", "j_bip_c_hips");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Spine, OdoroJointName.Chest, "spine", "spine1", "spine01", "j_bip_c_spine");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Chest, OdoroJointName.Neck, "chest", "upperchest", "spine2", "j_bip_c_chest", "j_bip_c_upperchest");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Neck, OdoroJointName.Head, "neck", "j_bip_c_neck");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftShoulder, OdoroJointName.LeftUpperArm, "leftshoulder", "lshoulder", "shoulderl", "j_bip_l_shoulder");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftUpperArm, OdoroJointName.LeftElbow, "leftupperarm", "leftarm", "lupperarm", "upperarml", "j_bip_l_upperarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist, "leftlowerarm", "leftforearm", "llowerarm", "forearml", "j_bip_l_lowerarm");

            TryAddNamedBinding(bindings, bones, OdoroJointName.RightShoulder, OdoroJointName.RightUpperArm, "rightshoulder", "rshoulder", "shoulderr", "j_bip_r_shoulder");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightUpperArm, OdoroJointName.RightElbow, "rightupperarm", "rightarm", "rupperarm", "upperarmr", "j_bip_r_upperarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightElbow, OdoroJointName.RightWrist, "rightlowerarm", "rightforearm", "rlowerarm", "forearmr", "j_bip_r_lowerarm");

            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftHip, OdoroJointName.LeftKnee, "leftupperleg", "leftupleg", "leftthigh", "lupperleg", "thighl", "j_bip_l_upperleg");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftKnee, OdoroJointName.LeftAnkle, "leftlowerleg", "leftleg", "leftcalf", "llowerleg", "calfl", "j_bip_l_lowerleg");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftAnkle, OdoroJointName.LeftFoot, "leftfoot", "lfoot", "footl", "j_bip_l_foot");

            TryAddNamedBinding(bindings, bones, OdoroJointName.RightHip, OdoroJointName.RightKnee, "rightupperleg", "rightupleg", "rightthigh", "rupperleg", "thighr", "j_bip_r_upperleg");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightKnee, OdoroJointName.RightAnkle, "rightlowerleg", "rightleg", "rightcalf", "rlowerleg", "calfr", "j_bip_r_lowerleg");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightAnkle, OdoroJointName.RightFoot, "rightfoot", "rfoot", "footr", "j_bip_r_foot");

            return bindings;
        }

        private static void CollectBones(Transform transform, Dictionary<string, Transform> bones)
        {
            if (transform == null)
            {
                return;
            }

            var normalized = NormalizeBoneName(transform.name);
            if (!string.IsNullOrEmpty(normalized) && !bones.ContainsKey(normalized))
            {
                bones.Add(normalized, transform);
            }

            for (var childIndex = 0; childIndex < transform.childCount; childIndex += 1)
            {
                CollectBones(transform.GetChild(childIndex), bones);
            }
        }

        private static void TryAddNamedBinding(
            List<BoneBinding> bindings,
            Dictionary<string, Transform> bones,
            OdoroJointName startJoint,
            OdoroJointName endJoint,
            params string[] aliases
        )
        {
            for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex += 1)
            {
                if (bones.TryGetValue(NormalizeBoneName(aliases[aliasIndex]), out var bone))
                {
                    AddBinding(bindings, bone, startJoint, endJoint);
                    return;
                }
            }
        }

        private static void TryAddBinding(
            List<BoneBinding> bindings,
            Animator animator,
            HumanBodyBones humanBone,
            OdoroJointName startJoint,
            OdoroJointName endJoint,
            bool mirrorArmTargetJoints = false
        )
        {
            var bone = animator.GetBoneTransform(humanBone);
            if (bone == null)
            {
                return;
            }

            AddBinding(bindings, bone, startJoint, endJoint, mirrorArmTargetJoints);
        }

        private static void AddBinding(
            List<BoneBinding> bindings,
            Transform bone,
            OdoroJointName startJoint,
            OdoroJointName endJoint,
            bool mirrorArmTargetJoints = false
        )
        {
            var child = FirstChildBone(bone);
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
                mirrorArmTargetJoints = mirrorArmTargetJoints,
            });
        }

        private static Transform FindRootBone(Transform root)
        {
            var bones = new Dictionary<string, Transform>();
            CollectBones(root, bones);
            return bones.TryGetValue("hips", out var hips)
                || bones.TryGetValue("pelvis", out hips)
                || bones.TryGetValue(NormalizeBoneName("j_bip_c_hips"), out hips)
                ? hips
                : root;
        }

        private static string NormalizeBoneName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .ToLowerInvariant()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("mixamorig:", string.Empty)
                .Replace("mixamorig", string.Empty);
        }

        private static void NormalizeBoundsForStage(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex += 1)
            {
                bounds.Encapsulate(renderers[rendererIndex].bounds);
            }

            if (bounds.size.y <= 0.001f)
            {
                return;
            }

            var scale = 1.7f / bounds.size.y;
            instance.transform.localScale *= scale;
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

            var targetStartJoint = ResolveTargetJoint(binding, startJoint);
            var targetEndJoint = ResolveTargetJoint(binding, endJoint);
            var targetDirection = TargetWorldDirection(frame, targetStartJoint, targetEndJoint);
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

        private OdoroJointName ResolveTargetJoint(BoneBinding binding, OdoroJointName jointName)
        {
            var shouldSwapArmJoint = binding.mirrorArmTargetJoints ^ debugSwapArmJoints;
            return shouldSwapArmJoint ? DebugArmSwapJoint(jointName) : jointName;
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

        private static OdoroJointName DebugArmSwapJoint(OdoroJointName jointName)
        {
            return jointName switch
            {
                OdoroJointName.LeftShoulder => OdoroJointName.RightShoulder,
                OdoroJointName.RightShoulder => OdoroJointName.LeftShoulder,
                OdoroJointName.LeftUpperArm => OdoroJointName.RightUpperArm,
                OdoroJointName.RightUpperArm => OdoroJointName.LeftUpperArm,
                OdoroJointName.LeftElbow => OdoroJointName.RightElbow,
                OdoroJointName.RightElbow => OdoroJointName.LeftElbow,
                OdoroJointName.LeftWrist => OdoroJointName.RightWrist,
                OdoroJointName.RightWrist => OdoroJointName.LeftWrist,
                _ => jointName,
            };
        }
    }
}
