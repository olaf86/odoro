using System;
using System.Linq;
using UnityEngine;

namespace Odoro
{
    public enum OdoroJointName
    {
        Root,
        Spine,
        Chest,
        Neck,
        Head,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
        LeftWrist,
        RightWrist,
        LeftHip,
        RightHip,
        LeftKnee,
        RightKnee,
        LeftAnkle,
        RightAnkle,
        LeftFoot,
        RightFoot,
        LeftUpperArm,
        RightUpperArm,
    }

    public enum OdoroJointStatus
    {
        Observed,
        Mapped,
        Derived,
        Inferred,
        Missing,
    }

    public static class OdoroSkeletonDefinition
    {
        public const string Id = "odoro.body.v1";
        public static readonly OdoroJointName[] JointNames =
            Enum.GetValues(typeof(OdoroJointName)).Cast<OdoroJointName>().ToArray();

        public static int JointCount => JointNames.Length;

        public static string RawValue(this OdoroJointName jointName)
        {
            return jointName switch
            {
                OdoroJointName.Root => "root",
                OdoroJointName.Spine => "spine",
                OdoroJointName.Chest => "chest",
                OdoroJointName.Neck => "neck",
                OdoroJointName.Head => "head",
                OdoroJointName.LeftShoulder => "leftShoulder",
                OdoroJointName.RightShoulder => "rightShoulder",
                OdoroJointName.LeftElbow => "leftElbow",
                OdoroJointName.RightElbow => "rightElbow",
                OdoroJointName.LeftWrist => "leftWrist",
                OdoroJointName.RightWrist => "rightWrist",
                OdoroJointName.LeftHip => "leftHip",
                OdoroJointName.RightHip => "rightHip",
                OdoroJointName.LeftKnee => "leftKnee",
                OdoroJointName.RightKnee => "rightKnee",
                OdoroJointName.LeftAnkle => "leftAnkle",
                OdoroJointName.RightAnkle => "rightAnkle",
                OdoroJointName.LeftFoot => "leftFoot",
                OdoroJointName.RightFoot => "rightFoot",
                OdoroJointName.LeftUpperArm => "leftUpperArm",
                OdoroJointName.RightUpperArm => "rightUpperArm",
                _ => jointName.ToString(),
            };
        }

        public static string RawValue(this OdoroJointStatus status)
        {
            return status switch
            {
                OdoroJointStatus.Observed => "observed",
                OdoroJointStatus.Mapped => "mapped",
                OdoroJointStatus.Derived => "derived",
                OdoroJointStatus.Inferred => "inferred",
                OdoroJointStatus.Missing => "missing",
                _ => status.ToString(),
            };
        }

        public static int IndexOf(OdoroJointName jointName)
        {
            return Array.IndexOf(JointNames, jointName);
        }

        public static string[] RawJointNames()
        {
            var values = new string[JointCount];
            for (var i = 0; i < JointCount; i += 1)
            {
                values[i] = JointNames[i].RawValue();
            }

            return values;
        }
    }
}
