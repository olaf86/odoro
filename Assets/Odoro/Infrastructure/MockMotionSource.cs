using System;
using UnityEngine;

namespace Odoro
{
    public sealed class MockMotionSource : IMotionSource
    {
        public CaptureMode CaptureMode => CaptureMode.Mock;
        public bool IsSupported => true;

        public event Action<MotionFrame> OnFrame;
        public event Action<string> OnStatusTextChanged;

        private bool active;
        private MotionSourceActivity currentActivity;
        private float lastEmitTime = -1f;
        private float startTime;
        private const float FrameInterval = 1f / 30f;

        public void Activate(MotionSourceActivity activity)
        {
            active = true;
            currentActivity = activity;
            startTime = Time.unscaledTime;
            lastEmitTime = -1f;
            OnStatusTextChanged?.Invoke(activity == MotionSourceActivity.Recording ? "録画ソースを有効化しました。" : "mock プレビューを表示しています。");
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
            OnFrame?.Invoke(MakeFrame(Mathf.Max(0f, now - startTime)));
        }

        private MotionFrame MakeFrame(float time)
        {
            var step = Mathf.Sin(time * 2.3f);
            var sway = Mathf.Sin(time * 1.4f);
            var armSwing = Mathf.Sin(time * 3.1f);
            var bounce = Mathf.Max(0f, Mathf.Sin(time * 4.2f)) * 0.08f;
            var activityBias = currentActivity == MotionSourceActivity.Recording ? 1f : 0.8f;

            var root = new Vector3(sway * 0.18f, 0.92f + bounce, 0f);
            var head = root + new Vector3(0f, 0.60f, 0f);
            var neck = Vector3.Lerp(root, head, 0.78f);
            var chest = Vector3.Lerp(root, neck, 0.68f);
            var spine = Vector3.Lerp(root, chest, 0.52f);
            var leftShoulder = chest + new Vector3(-0.20f, 0.04f, 0f);
            var rightShoulder = chest + new Vector3(0.20f, 0.04f, 0f);
            var leftUpperArm = leftShoulder + new Vector3(-0.10f, 0.02f + armSwing * 0.03f * activityBias, 0f);
            var rightUpperArm = rightShoulder + new Vector3(0.10f, 0.02f - armSwing * 0.03f * activityBias, 0f);
            var leftElbow = leftUpperArm + new Vector3(-0.12f, 0.03f + armSwing * 0.11f * activityBias, 0f);
            var rightElbow = rightUpperArm + new Vector3(0.12f, 0.03f - armSwing * 0.11f * activityBias, 0f);
            var leftWrist = leftElbow + new Vector3(-0.14f, -0.10f + armSwing * 0.08f * activityBias, 0f);
            var rightWrist = rightElbow + new Vector3(0.14f, -0.10f - armSwing * 0.08f * activityBias, 0f);
            var leftHip = root + new Vector3(-0.12f, -0.02f, 0f);
            var rightHip = root + new Vector3(0.12f, -0.02f, 0f);
            var leftKnee = leftHip + new Vector3(-0.02f, -0.34f + Mathf.Max(0f, step) * 0.10f, 0f);
            var rightKnee = rightHip + new Vector3(0.02f, -0.34f + Mathf.Max(0f, -step) * 0.10f, 0f);
            var leftFoot = leftKnee + new Vector3(0.03f, -0.35f, 0.08f);
            var rightFoot = rightKnee + new Vector3(0.03f, -0.35f, 0.08f);
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
