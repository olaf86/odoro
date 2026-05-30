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
            var cycle = time * 2.15f;
            var step = Mathf.Sin(cycle);
            var counterStep = Mathf.Sin(cycle + Mathf.PI);
            var sway = Mathf.Sin(cycle) * 0.5f + Mathf.Sin(time * 0.75f) * 0.5f;
            var torsoBreath = Mathf.Sin(time * 1.35f);
            var bounce = Mathf.Max(0f, Mathf.Sin(cycle * 2f)) * 0.006f;
            var activityBias = activity == MotionSourceActivity.Recording ? 1f : 0.8f;

            var yaw = sway * 0.025f;
            var right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            var forward = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var up = Vector3.up;

            var root = right * (sway * 0.018f) + forward * (Mathf.Sin(time * 0.6f) * 0.01f);
            root.y = 0.94f + bounce;

            var spine = root + up * 0.21f + forward * (torsoBreath * 0.004f);
            var chest = root + up * 0.42f + right * (sway * 0.006f) + forward * (torsoBreath * 0.008f);
            var neck = root + up * 0.58f + right * (sway * 0.008f) + forward * 0.012f;
            var head = root + up * 0.72f + right * (sway * 0.01f) + forward * 0.02f;

            var leftShoulder = chest - right * 0.18f + up * 0.055f + forward * 0.006f;
            var rightShoulder = chest + right * 0.18f + up * 0.055f + forward * 0.006f;

            var leftArmForward = -step * 0.045f * activityBias;
            var rightArmForward = -counterStep * 0.045f * activityBias;
            var leftElbowBend = 0.035f + Mathf.Max(0f, -step) * 0.012f * activityBias;
            var rightElbowBend = 0.035f + Mathf.Max(0f, -counterStep) * 0.012f * activityBias;

            var leftElbow = leftShoulder - right * 0.035f - up * 0.29f + forward * (leftArmForward + leftElbowBend);
            var rightElbow = rightShoulder + right * 0.035f - up * 0.29f + forward * (rightArmForward + rightElbowBend);
            var leftWrist = leftElbow + right * 0.02f - up * 0.285f + forward * (leftArmForward * 0.25f - 0.012f);
            var rightWrist = rightElbow - right * 0.02f - up * 0.285f + forward * (rightArmForward * 0.25f - 0.012f);
            var leftUpperArm = Vector3.Lerp(leftShoulder, leftElbow, 0.5f);
            var rightUpperArm = Vector3.Lerp(rightShoulder, rightElbow, 0.5f);

            var leftHip = root - right * 0.12f - up * 0.025f + forward * 0.005f;
            var rightHip = root + right * 0.12f - up * 0.025f - forward * 0.005f;
            var leftStride = step * 0.075f * activityBias;
            var rightStride = counterStep * 0.075f * activityBias;
            var leftKneeLift = Mathf.Max(0f, step) * 0.045f * activityBias;
            var rightKneeLift = Mathf.Max(0f, counterStep) * 0.045f * activityBias;
            var leftKnee = leftHip - up * (0.38f - leftKneeLift) - right * 0.015f + forward * leftStride;
            var rightKnee = rightHip - up * (0.38f - rightKneeLift) + right * 0.015f + forward * rightStride;
            var leftFoot = leftKnee - up * 0.37f + right * 0.02f + forward * (0.06f + leftStride * 0.55f);
            var rightFoot = rightKnee - up * 0.37f + right * 0.02f + forward * (0.06f + rightStride * 0.55f);
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
