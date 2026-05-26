using System;
using UnityEngine;

namespace Odoro
{
    public sealed class RecordedMotionSource : IMotionSource
    {
        public CaptureMode CaptureMode { get; }
        public bool IsSupported => PlaybackClip != null && !PlaybackClip.IsEmpty;
        public string ReplayPath { get; }
        public MotionClip SourceClip { get; }
        public MotionClip PlaybackClip { get; }

        public event Action<MotionFrame> OnFrame;
        public event Action<string> OnStatusTextChanged;

        private bool active;
        private int nextFrameIndex;
        private float startTime;

        public RecordedMotionSource(MotionClip clip, string replayPath, CaptureMode captureMode = CaptureMode.RearBody3D)
        {
            SourceClip = clip;
            PlaybackClip = MotionPlaybackClipPreparer.Prepare(clip, captureMode);
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
            if (!active || PlaybackClip == null || PlaybackClip.IsEmpty)
            {
                return;
            }

            var duration = Mathf.Max(0.0001f, PlaybackClip.Duration);
            var playbackTime = (now - startTime) % duration;
            if (nextFrameIndex >= PlaybackClip.FrameCount || PlaybackClip.frames[nextFrameIndex].time > playbackTime)
            {
                nextFrameIndex = 0;
            }

            while (nextFrameIndex < PlaybackClip.FrameCount && PlaybackClip.frames[nextFrameIndex].time <= playbackTime)
            {
                OnFrame?.Invoke(PlaybackClip.frames[nextFrameIndex].CloneWithTime(now));
                nextFrameIndex += 1;
            }
        }
    }
}
