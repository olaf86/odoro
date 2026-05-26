using System;
using UnityEngine;

namespace Odoro
{
    public sealed class RecordedMotionSource : IMotionSource
    {
        public CaptureMode CaptureMode { get; }
        public bool IsSupported => clip != null && !clip.IsEmpty;
        public string ReplayPath { get; }

        public event Action<MotionFrame> OnFrame;
        public event Action<string> OnStatusTextChanged;

        private readonly MotionClip clip;
        private bool active;
        private int nextFrameIndex;
        private float startTime;

        public RecordedMotionSource(MotionClip clip, string replayPath, CaptureMode captureMode = CaptureMode.RearBody3D)
        {
            this.clip = clip;
            ReplayPath = replayPath;
            CaptureMode = captureMode;
        }

        public void Activate(MotionSourceActivity activity)
        {
            active = IsSupported;
            nextFrameIndex = 0;
            startTime = Time.unscaledTime;
            OnStatusTextChanged?.Invoke(active
                ? "Replaying debug motion capture."
                : "No debug motion replay is available.");
        }

        public void Deactivate()
        {
            active = false;
        }

        public void Tick(float now)
        {
            if (!active || clip == null || clip.IsEmpty)
            {
                return;
            }

            var duration = Mathf.Max(0.0001f, clip.Duration);
            var playbackTime = (now - startTime) % duration;
            if (nextFrameIndex >= clip.FrameCount || clip.frames[nextFrameIndex].time > playbackTime)
            {
                nextFrameIndex = 0;
            }

            while (nextFrameIndex < clip.FrameCount && clip.frames[nextFrameIndex].time <= playbackTime)
            {
                OnFrame?.Invoke(clip.frames[nextFrameIndex].CloneWithTime(now));
                nextFrameIndex += 1;
            }
        }
    }
}
