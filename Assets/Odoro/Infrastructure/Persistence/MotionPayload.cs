using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    [Serializable]
    public struct MotionPayloadVector3
    {
        public float x;
        public float y;
        public float z;

        public MotionPayloadVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 VectorValue => new Vector3(x, y, z);
    }

    [Serializable]
    public struct MotionPayloadQuaternion
    {
        public float ix;
        public float iy;
        public float iz;
        public float r;

        public MotionPayloadQuaternion(MotionJointRotation rotation)
        {
            ix = rotation.ix;
            iy = rotation.iy;
            iz = rotation.iz;
            r = rotation.r;
        }

        public MotionJointRotation MotionValue => new MotionJointRotation
        {
            ix = ix,
            iy = iy,
            iz = iz,
            r = r,
        };
    }

    [Serializable]
    public sealed class MotionPayloadFrame
    {
        public float timeSeconds;
        public float timeBeats;
        public MotionPayloadVector3[] positions;
        public MotionPayloadQuaternion[] rotations;
        public float[] confidences;
        public string[] jointStatuses;
    }

    [Serializable]
    public sealed class MotionPayload
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion;
        public string skeletonId;
        public string[] jointNames;
        public int jointCount;
        public CaptureMode captureMode;
        public string sourcePlatform;
        public string sourceBackend;
        public MotionPayloadFrame[] frames;

        public static MotionPayload FromClip(
            MotionClip clip,
            CaptureMode captureMode,
            MotionRecordingContext recordingContext,
            string sourcePlatform,
            string sourceBackend,
            bool clipIsCanonical = false
        )
        {
            var canonicalClip = clipIsCanonical ? clip : OdoroCanonicalPoseMapper.CanonicalizedClip(clip);
            var payloadFrames = new MotionPayloadFrame[canonicalClip.frames.Count];
            var rawJointNames = OdoroSkeletonDefinition.RawJointNames();

            for (var i = 0; i < canonicalClip.frames.Count; i += 1)
            {
                var frame = canonicalClip.frames[i];
                var positions = new MotionPayloadVector3[frame.jointPositions.Length];
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    positions[jointIndex] = new MotionPayloadVector3(frame.jointPositions[jointIndex]);
                }

                var statuses = new string[positions.Length];
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    statuses[jointIndex] = OdoroJointStatus.Observed.RawValue();
                }

                payloadFrames[i] = new MotionPayloadFrame
                {
                    timeSeconds = frame.time,
                    timeBeats = frame.time * recordingContext.bpm / 60f,
                    positions = positions,
                    rotations = null,
                    confidences = null,
                    jointStatuses = statuses,
                };
            }

            return new MotionPayload
            {
                schemaVersion = CurrentSchemaVersion,
                skeletonId = OdoroSkeletonDefinition.Id,
                jointNames = rawJointNames,
                jointCount = OdoroSkeletonDefinition.JointCount,
                captureMode = captureMode,
                sourcePlatform = sourcePlatform,
                sourceBackend = sourceBackend,
                frames = payloadFrames,
            };
        }

        public MotionClip ToMotionClip()
        {
            var clip = new MotionClip();
            if (frames == null)
            {
                return clip;
            }

            for (var i = 0; i < frames.Length; i += 1)
            {
                var payloadFrame = frames[i];
                var positions = new Vector3[payloadFrame.positions.Length];
                for (var jointIndex = 0; jointIndex < positions.Length; jointIndex += 1)
                {
                    positions[jointIndex] = payloadFrame.positions[jointIndex].VectorValue;
                }

                clip.frames.Add(new MotionFrame
                {
                    time = payloadFrame.timeSeconds,
                    jointPositions = positions,
                    jointRotations = null,
                });
            }

            return clip;
        }
    }
}
