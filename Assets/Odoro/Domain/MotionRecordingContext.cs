using System;
using UnityEngine;

namespace Odoro
{
    public enum TempoSourceType
    {
        Metronome,
        AudioAsset,
    }

    [Serializable]
    public struct MotionRecordingContext
    {
        public const int FixedCaptureBarCount = 2;

        public TempoSourceType tempoSourceType;
        public string audioAssetReference;
        public float bpm;
        public int timeSignatureNumerator;
        public int timeSignatureDenominator;
        public int targetBarCount;
        public int countInBarCount;
        public string notes;

        public float BeatLength => targetBarCount * timeSignatureNumerator;
        public float FixedCaptureBeatLength => FixedCaptureBarCount * timeSignatureNumerator;
        public float FixedCaptureDuration => bpm <= 0f ? 0f : FixedCaptureBeatLength * 60f / bpm;

        public MotionRecordingContext NormalizedForFixedCaptureLength()
        {
            var normalized = this;
            normalized.targetBarCount = FixedCaptureBarCount;
            return normalized;
        }

        public static MotionRecordingContext DefaultMetronomeLoop => new MotionRecordingContext
        {
            tempoSourceType = TempoSourceType.Metronome,
            audioAssetReference = null,
            bpm = 120f,
            timeSignatureNumerator = 4,
            timeSignatureDenominator = 4,
            targetBarCount = FixedCaptureBarCount,
            countInBarCount = 1,
            notes = null,
        };
    }
}
