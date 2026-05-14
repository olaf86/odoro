using System;

namespace Odoro
{
    public enum MotionTakeUploadStatus
    {
        LocalOnly,
        PendingUpload,
        Uploaded,
        Failed,
    }

    [Serializable]
    public sealed class RecordingSessionRecord
    {
        public string id;
        public long createdAtTicks;
        public TempoSourceType tempoSourceType;
        public string audioAssetReference;
        public float bpm;
        public int timeSignatureNumerator;
        public int timeSignatureDenominator;
        public int targetBarCount;
        public int countInBarCount;
        public string notes;

        public MotionRecordingContext RecordingContext => new MotionRecordingContext
        {
            tempoSourceType = tempoSourceType,
            audioAssetReference = audioAssetReference,
            bpm = bpm,
            timeSignatureNumerator = timeSignatureNumerator,
            timeSignatureDenominator = timeSignatureDenominator,
            targetBarCount = targetBarCount,
            countInBarCount = countInBarCount,
            notes = notes,
        };

        public static RecordingSessionRecord Create(MotionRecordingContext recordingContext)
        {
            return new RecordingSessionRecord
            {
                id = Guid.NewGuid().ToString("N"),
                createdAtTicks = DateTime.UtcNow.Ticks,
                tempoSourceType = recordingContext.tempoSourceType,
                audioAssetReference = recordingContext.audioAssetReference,
                bpm = recordingContext.bpm,
                timeSignatureNumerator = recordingContext.timeSignatureNumerator,
                timeSignatureDenominator = recordingContext.timeSignatureDenominator,
                targetBarCount = recordingContext.targetBarCount,
                countInBarCount = recordingContext.countInBarCount,
                notes = recordingContext.notes,
            };
        }
    }

    [Serializable]
    public sealed class MotionTakeRecord
    {
        public string id;
        public long createdAtTicks;
        public string clipName;
        public int takeIndex;
        public CaptureMode captureMode;
        public float durationSeconds;
        public int frameCount;
        public float nominalFrameRate;
        public int barLength;
        public float beatLength;
        public float startBeatOffset;
        public bool isAccepted;
        public string localFilePath;
        public MotionTakeUploadStatus uploadStatus;
        public string remoteObjectKey;
        public int schemaVersion;
        public string sessionId;
    }

    [Serializable]
    public sealed class MotionTakeSummary
    {
        public string id;
        public string sessionId;
        public long createdAtTicks;
        public string clipName;
        public int takeIndex;
        public CaptureMode captureMode;
        public float durationSeconds;
        public int frameCount;
        public float nominalFrameRate;
        public int barLength;
        public float beatLength;
        public float startBeatOffset;
        public bool isAccepted;
        public string localFilePath;
        public MotionTakeUploadStatus uploadStatus;
        public string remoteObjectKey;
        public int schemaVersion;
        public TempoSourceType tempoSourceType;
        public string audioAssetReference;
        public float bpm;
        public int timeSignatureNumerator;
        public int timeSignatureDenominator;
        public int countInBarCount;

        public string DisplayName => string.IsNullOrEmpty(clipName) ? $"Take {takeIndex:00}" : clipName;

        public string SecondarySummary
        {
            get
            {
                var createdAt = new DateTime(createdAtTicks, DateTimeKind.Utc).ToLocalTime();
                return $"{createdAt:MM/dd HH:mm}  •  {durationSeconds:0.00}s  •  {frameCount} frames  •  {bpm:0} BPM";
            }
        }

        public MotionRecordingContext RecordingContext => new MotionRecordingContext
        {
            tempoSourceType = tempoSourceType,
            audioAssetReference = audioAssetReference,
            bpm = bpm,
            timeSignatureNumerator = timeSignatureNumerator,
            timeSignatureDenominator = timeSignatureDenominator,
            targetBarCount = barLength,
            countInBarCount = countInBarCount,
            notes = null,
        };
    }

    [Serializable]
    public sealed class MotionTakeSaveResult
    {
        public string sessionId;
        public string takeId;
        public string localFilePath;
    }
}
