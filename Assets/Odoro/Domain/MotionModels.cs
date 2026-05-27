using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public enum StudioScreen
    {
        Capture,
        RecordingSettings,
        ClipsLibrary,
        Stage,
        ModelSelection,
    }

    public enum MotionSourceActivity
    {
        Preview,
        Recording,
    }

    public enum CaptureMode
    {
        RearBody3D,
        FrontUpperBody,
        ImportedVideo,
        Mock,
    }

    [Serializable]
    public struct MotionJointRotation
    {
        public float ix;
        public float iy;
        public float iz;
        public float r;

        public MotionJointRotation(Quaternion quaternion)
        {
            ix = quaternion.x;
            iy = quaternion.y;
            iz = quaternion.z;
            r = quaternion.w;
        }

        public Quaternion QuaternionValue => new Quaternion(ix, iy, iz, r);
    }

    [Serializable]
    public sealed class MotionFrame
    {
        public float time;
        public Vector3[] jointPositions;
        public MotionJointRotation[] jointRotations;

        public MotionFrame CloneWithTime(float newTime)
        {
            var positions = jointPositions == null ? Array.Empty<Vector3>() : (Vector3[])jointPositions.Clone();
            var rotations = jointRotations == null ? null : (MotionJointRotation[])jointRotations.Clone();

            return new MotionFrame
            {
                time = newTime,
                jointPositions = positions,
                jointRotations = rotations,
            };
        }
    }

    [Serializable]
    public sealed class MotionClip
    {
        public List<MotionFrame> frames = new List<MotionFrame>();

        public int FrameCount => frames.Count;
        public float Duration => FrameCount == 0 ? 0f : frames[FrameCount - 1].time;
        public bool IsEmpty => FrameCount == 0;
        public float EstimatedFrameRate => FrameCount <= 1 || Duration <= 0f ? 0f : (FrameCount - 1) / Duration;

        public MotionFrame Sample(float sampleTime)
        {
            if (FrameCount == 0)
            {
                return null;
            }

            if (FrameCount == 1 || sampleTime <= frames[0].time)
            {
                return frames[0];
            }

            for (var index = 1; index < FrameCount; index += 1)
            {
                var next = frames[index];
                if (sampleTime > next.time)
                {
                    continue;
                }

                var previous = frames[index - 1];
                var span = Mathf.Max(0.0001f, next.time - previous.time);
                var blend = Mathf.Clamp01((sampleTime - previous.time) / span);

                return Interpolate(previous, next, blend);
            }

            return frames[FrameCount - 1];
        }

        private static MotionFrame Interpolate(MotionFrame from, MotionFrame to, float blend)
        {
            var positions = new Vector3[Mathf.Min(from.jointPositions.Length, to.jointPositions.Length)];
            for (var i = 0; i < positions.Length; i += 1)
            {
                positions[i] = Vector3.Lerp(from.jointPositions[i], to.jointPositions[i], blend);
            }

            return new MotionFrame
            {
                time = Mathf.Lerp(from.time, to.time, blend),
                jointPositions = positions,
                jointRotations = null,
            };
        }
    }

    public sealed class MotionStudioState
    {
        public string statusText;
        public bool isRecording;
        public bool isPlaying;
        public int recordedFrameCount;
        public float recordingDuration;
        public float clipDuration;
        public bool hasClip;

        public static MotionStudioState Default()
        {
            return new MotionStudioState
            {
                statusText = StudioL10n.StatusPreviewIdle,
                isRecording = false,
                isPlaying = false,
                recordedFrameCount = 0,
                recordingDuration = 0f,
                clipDuration = 0f,
                hasClip = false,
            };
        }
    }
}
