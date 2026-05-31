using System;
using UnityEngine;

namespace Odoro
{
    public enum MockMotionPattern
    {
        TPose,
        Walk,
        Run,
        LeftStep,
        KneeLift,
    }

    public sealed class MockMotionSource : IMotionSource
    {
        public const float DefaultClipFrameRate = 30f;

        public CaptureMode CaptureMode => CaptureMode.Mock;
        public bool IsSupported => true;
        public MockMotionPattern Pattern { get; private set; } = MockMotionPattern.TPose;
        public string PatternLabel => PatternDisplayName(Pattern);

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
            OnFrame?.Invoke(CreateFrame(Mathf.Max(0f, now - startTime), currentActivity, Pattern));
        }

        public void CyclePattern()
        {
            Pattern = Pattern switch
            {
                MockMotionPattern.TPose => MockMotionPattern.Walk,
                MockMotionPattern.Walk => MockMotionPattern.Run,
                MockMotionPattern.Run => MockMotionPattern.LeftStep,
                MockMotionPattern.LeftStep => MockMotionPattern.KneeLift,
                _ => MockMotionPattern.TPose,
            };

            startTime = Time.unscaledTime;
            lastEmitTime = -1f;
            var statusText = currentActivity == MotionSourceActivity.Recording
                ? StudioL10n.StatusMockRecording
                : StudioL10n.StatusMockPreview;
            OnStatusTextChanged?.Invoke($"{statusText} ({PatternLabel})");
        }

        public static MotionClip CreateClip(float duration, MotionSourceActivity activity)
        {
            return CreateClip(duration, activity, MockMotionPattern.TPose);
        }

        public static MotionClip CreateClip(float duration, MotionSourceActivity activity, MockMotionPattern pattern)
        {
            var clip = new MotionClip();
            var frameCount = Mathf.Max(2, Mathf.CeilToInt(duration * DefaultClipFrameRate));
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex += 1)
            {
                var time = frameIndex / DefaultClipFrameRate;
                clip.frames.Add(CreateFrame(time, activity, pattern));
            }

            return clip;
        }

        public static MotionFrame CreateFrame(float time, MotionSourceActivity activity)
        {
            return CreateFrame(time, activity, MockMotionPattern.TPose);
        }

        public static MotionFrame CreateFrame(float time, MotionSourceActivity activity, MockMotionPattern pattern)
        {
            _ = activity;
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

            ApplyPattern(
                pattern,
                time,
                right,
                up,
                forward,
                ref root,
                ref spine,
                ref chest,
                ref neck,
                ref head,
                ref leftShoulder,
                ref rightShoulder,
                ref leftHip,
                ref rightHip,
                ref leftUpperArm,
                ref rightUpperArm,
                ref leftElbow,
                ref rightElbow,
                ref leftWrist,
                ref rightWrist,
                ref leftKnee,
                ref rightKnee,
                ref leftAnkle,
                ref rightAnkle,
                ref leftFoot,
                ref rightFoot
            );

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

        private static void ApplyPattern(
            MockMotionPattern pattern,
            float time,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            ref Vector3 root,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder,
            ref Vector3 leftHip,
            ref Vector3 rightHip,
            ref Vector3 leftUpperArm,
            ref Vector3 rightUpperArm,
            ref Vector3 leftElbow,
            ref Vector3 rightElbow,
            ref Vector3 leftWrist,
            ref Vector3 rightWrist,
            ref Vector3 leftKnee,
            ref Vector3 rightKnee,
            ref Vector3 leftAnkle,
            ref Vector3 rightAnkle,
            ref Vector3 leftFoot,
            ref Vector3 rightFoot
        )
        {
            switch (pattern)
            {
                case MockMotionPattern.Walk:
                    ApplyWalkCycle(
                        time,
                        right,
                        up,
                        forward,
                        ref root,
                        ref spine,
                        ref chest,
                        ref neck,
                        ref head,
                        ref leftShoulder,
                        ref rightShoulder,
                        ref leftHip,
                        ref rightHip,
                        ref leftUpperArm,
                        ref rightUpperArm,
                        ref leftElbow,
                        ref rightElbow,
                        ref leftWrist,
                        ref rightWrist,
                        ref leftKnee,
                        ref rightKnee,
                        ref leftAnkle,
                        ref rightAnkle,
                        ref leftFoot,
                        ref rightFoot
                    );
                    break;
                case MockMotionPattern.Run:
                    ApplyRunCycle(
                        time,
                        right,
                        up,
                        forward,
                        ref root,
                        ref spine,
                        ref chest,
                        ref neck,
                        ref head,
                        ref leftShoulder,
                        ref rightShoulder,
                        ref leftHip,
                        ref rightHip,
                        ref leftUpperArm,
                        ref rightUpperArm,
                        ref leftElbow,
                        ref rightElbow,
                        ref leftWrist,
                        ref rightWrist,
                        ref leftKnee,
                        ref rightKnee,
                        ref leftAnkle,
                        ref rightAnkle,
                        ref leftFoot,
                        ref rightFoot
                    );
                    break;
                case MockMotionPattern.LeftStep:
                    ApplyLeftStep(
                        time,
                        right,
                        up,
                        forward,
                        ref root,
                        ref spine,
                        ref chest,
                        ref neck,
                        ref head,
                        ref leftShoulder,
                        ref rightShoulder,
                        ref leftHip,
                        ref rightHip,
                        ref leftUpperArm,
                        ref rightUpperArm,
                        ref leftElbow,
                        ref rightElbow,
                        ref leftWrist,
                        ref rightWrist,
                        ref leftKnee,
                        ref rightKnee,
                        ref leftAnkle,
                        ref rightAnkle,
                        ref leftFoot,
                        ref rightFoot
                    );
                    break;
                case MockMotionPattern.KneeLift:
                    ApplyKneeLift(
                        time,
                        right,
                        up,
                        forward,
                        leftShoulder,
                        rightShoulder,
                        leftHip,
                        ref leftUpperArm,
                        ref rightUpperArm,
                        ref leftElbow,
                        ref rightElbow,
                        ref leftWrist,
                        ref rightWrist,
                        ref leftKnee,
                        ref leftAnkle,
                        ref leftFoot
                    );
                    break;
            }
        }

        private static void ApplyWalkCycle(
            float time,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            ref Vector3 root,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder,
            ref Vector3 leftHip,
            ref Vector3 rightHip,
            ref Vector3 leftUpperArm,
            ref Vector3 rightUpperArm,
            ref Vector3 leftElbow,
            ref Vector3 rightElbow,
            ref Vector3 leftWrist,
            ref Vector3 rightWrist,
            ref Vector3 leftKnee,
            ref Vector3 rightKnee,
            ref Vector3 leftAnkle,
            ref Vector3 rightAnkle,
            ref Vector3 leftFoot,
            ref Vector3 rightFoot
        )
        {
            var phase = Mathf.Sin(time * Mathf.PI * 2f * 1.1f);
            var opposite = -phase;
            var lift = Mathf.Abs(phase) * 0.035f;
            OffsetTorso(
                up * lift,
                ref root,
                ref spine,
                ref chest,
                ref neck,
                ref head,
                ref leftShoulder,
                ref rightShoulder,
                ref leftHip,
                ref rightHip
            );

            SetLegPose(leftHip, forward, up, phase * 0.26f, Mathf.Max(0f, phase) * 0.10f, Mathf.Abs(phase) * 0.08f, out leftKnee, out leftAnkle, out leftFoot);
            SetLegPose(rightHip, forward, up, opposite * 0.26f, Mathf.Max(0f, opposite) * 0.10f, Mathf.Abs(opposite) * 0.08f, out rightKnee, out rightAnkle, out rightFoot);

            SetArmPose(leftShoulder, -right, forward, up, opposite * 0.32f, out leftUpperArm, out leftElbow, out leftWrist);
            SetArmPose(rightShoulder, right, forward, up, phase * 0.32f, out rightUpperArm, out rightElbow, out rightWrist);
        }

        private static void ApplyRunCycle(
            float time,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            ref Vector3 root,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder,
            ref Vector3 leftHip,
            ref Vector3 rightHip,
            ref Vector3 leftUpperArm,
            ref Vector3 rightUpperArm,
            ref Vector3 leftElbow,
            ref Vector3 rightElbow,
            ref Vector3 leftWrist,
            ref Vector3 rightWrist,
            ref Vector3 leftKnee,
            ref Vector3 rightKnee,
            ref Vector3 leftAnkle,
            ref Vector3 rightAnkle,
            ref Vector3 leftFoot,
            ref Vector3 rightFoot
        )
        {
            var phase = Mathf.Sin(time * Mathf.PI * 2f * 1.8f);
            var opposite = -phase;
            var lift = Mathf.Abs(phase) * 0.050f;
            OffsetTorso(
                up * lift,
                ref root,
                ref spine,
                ref chest,
                ref neck,
                ref head,
                ref leftShoulder,
                ref rightShoulder,
                ref leftHip,
                ref rightHip
            );
            OffsetUpperBody(forward * 0.12f - up * 0.015f, ref spine, ref chest, ref neck, ref head, ref leftShoulder, ref rightShoulder);

            SetLegPose(leftHip, forward, up, phase * 0.34f, Mathf.Max(0f, phase) * 0.16f, Mathf.Abs(phase) * 0.12f, out leftKnee, out leftAnkle, out leftFoot);
            SetLegPose(rightHip, forward, up, opposite * 0.34f, Mathf.Max(0f, opposite) * 0.16f, Mathf.Abs(opposite) * 0.12f, out rightKnee, out rightAnkle, out rightFoot);

            SetRunArmPose(leftShoulder, -right, forward, up, opposite * 0.48f, out leftUpperArm, out leftElbow, out leftWrist);
            SetRunArmPose(rightShoulder, right, forward, up, phase * 0.48f, out rightUpperArm, out rightElbow, out rightWrist);
        }

        private static void ApplyLeftStep(
            float time,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            ref Vector3 root,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder,
            ref Vector3 leftHip,
            ref Vector3 rightHip,
            ref Vector3 leftUpperArm,
            ref Vector3 rightUpperArm,
            ref Vector3 leftElbow,
            ref Vector3 rightElbow,
            ref Vector3 leftWrist,
            ref Vector3 rightWrist,
            ref Vector3 leftKnee,
            ref Vector3 rightKnee,
            ref Vector3 leftAnkle,
            ref Vector3 rightAnkle,
            ref Vector3 leftFoot,
            ref Vector3 rightFoot
        )
        {
            var pulse = SmoothPulse(time, 0.65f);
            OffsetTorso(
                forward * (0.06f * pulse) + right * (0.035f * pulse) + up * (0.035f * pulse),
                ref root,
                ref spine,
                ref chest,
                ref neck,
                ref head,
                ref leftShoulder,
                ref rightShoulder,
                ref leftHip,
                ref rightHip
            );
            OffsetUpperBody(forward * (0.035f * pulse), ref spine, ref chest, ref neck, ref head, ref leftShoulder, ref rightShoulder);

            SetLegPose(
                leftHip,
                forward,
                up,
                0.42f * pulse,
                0.12f * pulse,
                0.12f * pulse,
                out leftKnee,
                out leftAnkle,
                out leftFoot
            );
            SetLegPose(
                rightHip,
                forward,
                up,
                -0.18f * pulse,
                0f,
                0.04f * pulse,
                out rightKnee,
                out rightAnkle,
                out rightFoot
            );
            SetArmPose(leftShoulder, -right, forward, up, -0.36f * pulse, out leftUpperArm, out leftElbow, out leftWrist);
            SetArmPose(rightShoulder, right, forward, up, 0.40f * pulse, out rightUpperArm, out rightElbow, out rightWrist);
        }

        private static void ApplyKneeLift(
            float time,
            Vector3 right,
            Vector3 up,
            Vector3 forward,
            Vector3 leftShoulder,
            Vector3 rightShoulder,
            Vector3 leftHip,
            ref Vector3 leftUpperArm,
            ref Vector3 rightUpperArm,
            ref Vector3 leftElbow,
            ref Vector3 rightElbow,
            ref Vector3 leftWrist,
            ref Vector3 rightWrist,
            ref Vector3 leftKnee,
            ref Vector3 leftAnkle,
            ref Vector3 leftFoot
        )
        {
            var pulse = SmoothPulse(time, 0.70f);
            var relaxedKnee = leftHip - up * 0.38f;
            var relaxedAnkle = leftHip - up * 0.72f;
            var raisedKnee = leftHip + forward * 0.30f - up * 0.10f;
            var foldedAnkle = leftHip + forward * 0.16f - up * 0.42f;
            leftKnee = Vector3.Lerp(relaxedKnee, raisedKnee, pulse);
            leftAnkle = Vector3.Lerp(relaxedAnkle, foldedAnkle, pulse);
            leftFoot = leftAnkle + forward * Mathf.Lerp(0.06f, 0.12f, pulse) - up * 0.03f;
            SetArmPose(leftShoulder, -right, forward, up, -0.18f * pulse, out leftUpperArm, out leftElbow, out leftWrist);
            SetArmPose(rightShoulder, right, forward, up, 0.18f * pulse, out rightUpperArm, out rightElbow, out rightWrist);
        }

        private static void OffsetTorso(
            Vector3 offset,
            ref Vector3 root,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder,
            ref Vector3 leftHip,
            ref Vector3 rightHip
        )
        {
            root += offset;
            spine += offset;
            chest += offset;
            neck += offset;
            head += offset;
            leftShoulder += offset;
            rightShoulder += offset;
            leftHip += offset;
            rightHip += offset;
        }

        private static void OffsetUpperBody(
            Vector3 offset,
            ref Vector3 spine,
            ref Vector3 chest,
            ref Vector3 neck,
            ref Vector3 head,
            ref Vector3 leftShoulder,
            ref Vector3 rightShoulder
        )
        {
            spine += offset * 0.45f;
            chest += offset * 0.75f;
            neck += offset;
            head += offset;
            leftShoulder += offset * 0.75f;
            rightShoulder += offset * 0.75f;
        }

        private static void SetArmPose(
            Vector3 shoulder,
            Vector3 sideOut,
            Vector3 forward,
            Vector3 up,
            float swing,
            out Vector3 upperArm,
            out Vector3 elbow,
            out Vector3 wrist
        )
        {
            var swingOffset = forward * swing;
            upperArm = shoulder + sideOut * 0.115f - up * 0.16f + swingOffset * 0.35f;
            elbow = shoulder + sideOut * 0.150f - up * 0.34f + swingOffset * 0.75f;
            wrist = shoulder + sideOut * 0.125f - up * 0.55f + swingOffset;
        }

        private static void SetRunArmPose(
            Vector3 shoulder,
            Vector3 sideOut,
            Vector3 forward,
            Vector3 up,
            float swing,
            out Vector3 upperArm,
            out Vector3 elbow,
            out Vector3 wrist
        )
        {
            var upperSwing = forward * swing;
            upperArm = shoulder + sideOut * 0.120f - up * 0.12f + upperSwing * 0.30f;
            elbow = shoulder + sideOut * 0.175f - up * 0.27f + upperSwing * 0.75f;
            wrist = shoulder + sideOut * 0.130f - up * 0.18f - upperSwing * 0.20f;
        }

        private static void SetLegPose(
            Vector3 hip,
            Vector3 forward,
            Vector3 up,
            float step,
            float lift,
            float kneeBend,
            out Vector3 knee,
            out Vector3 ankle,
            out Vector3 foot
        )
        {
            knee = hip - up * 0.38f + forward * (step * 0.45f + kneeBend) + up * (lift * 0.45f);
            ankle = hip - up * 0.72f + forward * step + up * lift;
            foot = ankle + forward * 0.06f - up * 0.025f;
        }

        private static float SmoothPulse(float time, float frequency)
        {
            var phase = Mathf.Repeat(time * frequency, 1f);
            var triangle = phase < 0.5f ? phase * 2f : (1f - phase) * 2f;
            return Mathf.SmoothStep(0f, 1f, triangle);
        }

        private static string PatternDisplayName(MockMotionPattern pattern)
        {
            return pattern switch
            {
                MockMotionPattern.Run => "Run",
                MockMotionPattern.Walk => "Walk",
                MockMotionPattern.LeftStep => "Left Step",
                MockMotionPattern.KneeLift => "Knee Lift",
                _ => "T Pose",
            };
        }
    }
}
