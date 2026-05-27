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
        private readonly string displayName;

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

        private static List<BoneBinding> BuildHumanoidBindings(Animator animator)
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

        private static List<BoneBinding> BuildNamedTransformBindings(Transform root)
        {
            var bones = new Dictionary<string, Transform>();
            CollectBones(root, bones);

            var bindings = new List<BoneBinding>();
            TryAddNamedBinding(bindings, bones, OdoroJointName.Root, OdoroJointName.Spine, "hips", "pelvis", "j_bip_c_hips");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Spine, OdoroJointName.Chest, "spine", "spine1", "spine01", "j_bip_c_spine");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Chest, OdoroJointName.Neck, "chest", "upperchest", "spine2", "j_bip_c_chest", "j_bip_c_upperchest");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Neck, OdoroJointName.Head, "neck", "j_bip_c_neck");
            TryAddNamedBinding(bindings, bones, OdoroJointName.Neck, OdoroJointName.Head, "head", "j_bip_c_head");

            TryAddNamedBinding(bindings, bones, OdoroJointName.Chest, OdoroJointName.LeftShoulder, "leftshoulder", "lshoulder", "shoulderl", "j_bip_l_shoulder");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftShoulder, OdoroJointName.LeftElbow, "leftupperarm", "leftarm", "lupperarm", "upperarml", "j_bip_l_upperarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist, "leftlowerarm", "leftforearm", "llowerarm", "forearml", "j_bip_l_lowerarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.LeftElbow, OdoroJointName.LeftWrist, "lefthand", "lhand", "handl", "j_bip_l_hand");

            TryAddNamedBinding(bindings, bones, OdoroJointName.Chest, OdoroJointName.RightShoulder, "rightshoulder", "rshoulder", "shoulderr", "j_bip_r_shoulder");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightShoulder, OdoroJointName.RightElbow, "rightupperarm", "rightarm", "rupperarm", "upperarmr", "j_bip_r_upperarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightElbow, OdoroJointName.RightWrist, "rightlowerarm", "rightforearm", "rlowerarm", "forearmr", "j_bip_r_lowerarm");
            TryAddNamedBinding(bindings, bones, OdoroJointName.RightElbow, OdoroJointName.RightWrist, "righthand", "rhand", "handr", "j_bip_r_hand");

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
            OdoroJointName endJoint
        )
        {
            var bone = animator.GetBoneTransform(humanBone);
            if (bone == null)
            {
                return;
            }

            AddBinding(bindings, bone, startJoint, endJoint);
        }

        private static void AddBinding(
            List<BoneBinding> bindings,
            Transform bone,
            OdoroJointName startJoint,
            OdoroJointName endJoint
        )
        {
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
