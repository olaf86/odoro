using System;
using UnityEngine;

namespace Odoro
{
    public sealed class MockMotionSource : IMotionSource
    {
        public const float DefaultClipFrameRate = 30f;

        public CaptureMode CaptureMode => CaptureMode.Mock;
        public bool IsSupported => true;

        public event Action<MotionFrame> OnFrame;
        public event Action<string> OnStatusTextChanged;

        private bool active;
        private MotionSourceActivity currentActivity;
        private float lastEmitTime = -1f;
        private float startTime;
        private const float FrameInterval = 1f / DefaultClipFrameRate;

        public void Activate(MotionSourceActivity activity)
        {
            active = true;
            currentActivity = activity;
            startTime = Time.unscaledTime;
            lastEmitTime = -1f;
            OnStatusTextChanged?.Invoke(activity == MotionSourceActivity.Recording ? StudioL10n.StatusMockRecording : StudioL10n.StatusMockPreview);
        }

        public void Deactivate()
        {
            active = false;
        }

        public void Tick(float now)
        {
            if (!active)
            {
                return;
            }

            if (lastEmitTime >= 0f && now - lastEmitTime < FrameInterval)
            {
                return;
            }

            lastEmitTime = now;
            OnFrame?.Invoke(CreateFrame(Mathf.Max(0f, now - startTime), currentActivity));
        }

        public static MotionClip CreateClip(float duration, MotionSourceActivity activity)
        {
            var clip = new MotionClip();
            var frameCount = Mathf.Max(2, Mathf.CeilToInt(duration * DefaultClipFrameRate));
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex += 1)
            {
                var time = frameIndex / DefaultClipFrameRate;
                clip.frames.Add(CreateFrame(time, activity));
            }

            return clip;
        }

        public static MotionFrame CreateFrame(float time, MotionSourceActivity activity)
        {
            var breath = Mathf.Sin(time * 1.2f) * 0.004f;
            var right = Vector3.right;
            var forward = Vector3.forward;
            var up = Vector3.up;

            var root = new Vector3(0f, 0.94f + breath, 0f);

            var spine = root + up * 0.21f;
            var chest = root + up * 0.42f;
            var neck = root + up * 0.60f;
            var head = root + up * 0.74f;

            var leftShoulder = chest - right * 0.19f + up * 0.055f;
            var rightShoulder = chest + right * 0.19f + up * 0.055f;

            var armDrop = 0.015f;
            var leftUpperArm = leftShoulder - right * 0.24f - up * armDrop;
            var rightUpperArm = rightShoulder + right * 0.24f - up * armDrop;
            var leftElbow = leftUpperArm - right * 0.25f - up * armDrop;
            var rightElbow = rightUpperArm + right * 0.25f - up * armDrop;
            var leftWrist = leftElbow - right * 0.24f - up * armDrop;
            var rightWrist = rightElbow + right * 0.24f - up * armDrop;

            var leftHip = root - right * 0.12f - up * 0.025f;
            var rightHip = root + right * 0.12f - up * 0.025f;
            var leftKnee = leftHip - up * 0.39f;
            var rightKnee = rightHip - up * 0.39f;
            var leftFoot = leftKnee - up * 0.37f;
            var rightFoot = rightKnee - up * 0.37f;
            var leftAnkle = Vector3.Lerp(leftKnee, leftFoot, 0.92f);
            var rightAnkle = Vector3.Lerp(rightKnee, rightFoot, 0.92f);

            return new MotionFrame
            {
                time = time,
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
