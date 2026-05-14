using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class MotionStudioInteractor
    {
        public MotionStudioState State { get; } = MotionStudioState.Default();
        public MotionClip CurrentClip { get; private set; }
        public MotionClip SourceClip { get; private set; }

        public event Action<MotionStudioState> OnStateChanged;
        public event Action<MotionClip> OnClipChanged;
        public event Action<MotionClip> OnSourceClipChanged;
        public event Action<MotionClip> OnRecordingCompleted;

        private readonly IMotionSource source;
        private readonly List<MotionFrame> capturedFrames = new List<MotionFrame>();
        private float maximumCaptureDuration;
        private float firstFrameTimestamp = -1f;

        public MotionStudioInteractor(IMotionSource source, float maximumCaptureDuration)
        {
            this.source = source;
            this.maximumCaptureDuration = maximumCaptureDuration;
            source.OnFrame += Consume;
        }

        public void UpdateMaximumCaptureDuration(float duration)
        {
            maximumCaptureDuration = duration;
        }

        public void BeginRecording()
        {
            capturedFrames.Clear();
            firstFrameTimestamp = -1f;
            State.isRecording = true;
            State.recordedFrameCount = 0;
            State.recordingDuration = 0f;
            State.statusText = "モーションを記録しています。";
            PublishState();
        }

        public void StopRecording()
        {
            if (!State.isRecording)
            {
                return;
            }

            State.isRecording = false;

            if (capturedFrames.Count < 2)
            {
                State.statusText = "記録フレームが足りませんでした。";
                PublishState();
                return;
            }

            var sourceClip = new MotionClip();
            sourceClip.frames.AddRange(capturedFrames);
            SourceClip = sourceClip;
            OnSourceClipChanged?.Invoke(SourceClip);

            var canonicalClip = OdoroCanonicalPoseMapper.CanonicalizedClip(sourceClip);
            ReplaceCurrentClip(canonicalClip, sourceClip);

            State.statusText = "キャプチャが完了しました。";
            PublishState();
            OnRecordingCompleted?.Invoke(canonicalClip);
        }

        public void ReplaceCurrentClip(MotionClip clip, MotionClip sourceClip = null)
        {
            SourceClip = sourceClip ?? clip;
            CurrentClip = clip;
            State.hasClip = clip != null && !clip.IsEmpty;
            State.clipDuration = clip?.Duration ?? 0f;
            OnSourceClipChanged?.Invoke(SourceClip);
            OnClipChanged?.Invoke(CurrentClip);
            PublishState();
        }

        public void SetPlaybackActive(bool isPlaying)
        {
            State.isPlaying = isPlaying;
            PublishState();
        }

        public void SetStatusText(string statusText)
        {
            if (State.isRecording)
            {
                return;
            }

            State.statusText = statusText;
            PublishState();
        }

        private void Consume(MotionFrame frame)
        {
            if (!State.isRecording)
            {
                return;
            }

            if (firstFrameTimestamp < 0f)
            {
                firstFrameTimestamp = frame.time;
            }

            var relativeTime = Mathf.Max(0f, frame.time - firstFrameTimestamp);
            capturedFrames.Add(frame.CloneWithTime(relativeTime));
            State.recordedFrameCount = capturedFrames.Count;
            State.recordingDuration = Mathf.Min(maximumCaptureDuration, relativeTime);
            PublishState();

            if (relativeTime >= maximumCaptureDuration)
            {
                StopRecording();
            }
        }

        private void PublishState()
        {
            OnStateChanged?.Invoke(State);
        }
    }
}
