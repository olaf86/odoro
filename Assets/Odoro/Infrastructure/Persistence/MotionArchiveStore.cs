using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Odoro
{
    [Serializable]
    internal sealed class ArchiveIndex
    {
        public int schemaVersion = 1;
        public List<RecordingSessionRecord> sessions = new List<RecordingSessionRecord>();
        public List<MotionTakeRecord> takes = new List<MotionTakeRecord>();
    }

    public sealed class MotionArchiveStore
    {
        private readonly string rootPath;
        private readonly string indexPath;
        private readonly MotionPayloadFileStore payloadFileStore;
        private readonly MotionSourceClipFileStore sourceClipFileStore;

        public MotionArchiveStore()
        {
            rootPath = Path.Combine(Application.persistentDataPath, "OdoroArchiveV2");
            indexPath = Path.Combine(rootPath, "archive-index.json");
            payloadFileStore = new MotionPayloadFileStore();
            sourceClipFileStore = new MotionSourceClipFileStore();

            Directory.CreateDirectory(rootPath);
        }

        public MotionTakeSummary SaveTake(
            MotionClip sourceClip,
            CaptureMode captureMode,
            MotionRecordingContext recordingContext,
            string existingSessionId = null,
            float startBeatOffset = 0f
        )
        {
            var index = LoadIndex();
            var session = ResolveSession(index, recordingContext, existingSessionId);
            var takeId = Guid.NewGuid().ToString("N");
            var sourceBackend = captureMode == CaptureMode.Mock ? "mock" : captureMode.ToString();
            var artifacts = MotionTakeArtifactsBuilder.Build(sourceClip, captureMode);
            var payload = MotionPayload.FromClip(
                artifacts.playbackClip,
                captureMode,
                recordingContext,
                sourcePlatform: "Unity",
                sourceBackend: sourceBackend,
                clipIsCanonical: true
            );
            string localFilePath;
            try
            {
                localFilePath = payloadFileStore.Write(payload, takeId);
                sourceClipFileStore.Write(sourceClip, takeId, captureMode, "Unity", sourceBackend);
            }
            catch
            {
                payloadFileStore.Remove(takeId);
                sourceClipFileStore.Remove(takeId);
                throw;
            }
            var takeIndex = NextTakeIndex(index, session.id);

            var record = new MotionTakeRecord
            {
                id = takeId,
                createdAtTicks = DateTime.UtcNow.Ticks,
                clipName = $"Take {takeIndex:00}",
                takeIndex = takeIndex,
                captureMode = captureMode,
                durationSeconds = artifacts.playbackClip.Duration,
                frameCount = artifacts.playbackClip.FrameCount,
                nominalFrameRate = artifacts.playbackClip.EstimatedFrameRate,
                barLength = recordingContext.targetBarCount,
                beatLength = recordingContext.BeatLength,
                startBeatOffset = startBeatOffset,
                isAccepted = false,
                localFilePath = localFilePath,
                uploadStatus = MotionTakeUploadStatus.LocalOnly,
                remoteObjectKey = null,
                schemaVersion = MotionPayload.CurrentSchemaVersion,
                sessionId = session.id,
            };

            index.takes.Add(record);
            SaveIndex(index);
            return ToSummary(record, session);
        }

        public MotionClip LoadClip(string takeId)
        {
            return LoadStoredTake(takeId).clip;
        }

        public StoredMotionTake LoadStoredTake(string takeId)
        {
            var index = LoadIndex();
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].id != takeId)
                {
                    continue;
                }

                var payload = payloadFileStore.Read(index.takes[i].localFilePath);
                var playbackClip = payload.ToMotionClip();
                MotionClip sourceClip = null;
                if (sourceClipFileStore.Exists(takeId))
                {
                    sourceClip = sourceClipFileStore.Read(takeId);
                }

                return new StoredMotionTake
                {
                    clip = playbackClip,
                    sourceClip = sourceClip ?? playbackClip,
                };
            }

            throw new FileNotFoundException($"Take not found: {takeId}");
        }

        public RecordingSessionSummary FetchSessionSummary(string sessionId)
        {
            var index = LoadIndex();
            var session = FindSession(index, sessionId);
            if (session == null)
            {
                return null;
            }

            var takeCount = 0;
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].sessionId == sessionId)
                {
                    takeCount += 1;
                }
            }

            return new RecordingSessionSummary
            {
                id = session.id,
                createdAtTicks = session.createdAtTicks,
                bpm = session.bpm,
                timeSignatureNumerator = session.timeSignatureNumerator,
                timeSignatureDenominator = session.timeSignatureDenominator,
                targetBarCount = session.targetBarCount,
                countInBarCount = session.countInBarCount,
                takeCount = takeCount,
            };
        }

        public List<MotionTakeSummary> FetchAllTakeSummaries()
        {
            var index = LoadIndex();
            var summaries = new List<MotionTakeSummary>();
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                var session = FindSession(index, index.takes[i].sessionId);
                if (session != null)
                {
                    summaries.Add(ToSummary(index.takes[i], session));
                }
            }

            summaries.Sort((left, right) => right.createdAtTicks.CompareTo(left.createdAtTicks));
            return summaries;
        }

        public List<MotionTakeSummary> FetchTakeSummariesInSession(string sessionId)
        {
            var index = LoadIndex();
            var session = FindSession(index, sessionId);
            var summaries = new List<MotionTakeSummary>();
            if (session == null)
            {
                return summaries;
            }

            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].sessionId == sessionId)
                {
                    summaries.Add(ToSummary(index.takes[i], session));
                }
            }

            summaries.Sort((left, right) => left.takeIndex.CompareTo(right.takeIndex));
            return summaries;
        }

        private RecordingSessionRecord ResolveSession(
            ArchiveIndex index,
            MotionRecordingContext recordingContext,
            string existingSessionId
        )
        {
            var existing = FindSession(index, existingSessionId);
            if (existing != null)
            {
                return existing;
            }

            var session = RecordingSessionRecord.Create(recordingContext);
            index.sessions.Add(session);
            return session;
        }

        private static int NextTakeIndex(ArchiveIndex index, string sessionId)
        {
            var takeIndex = 1;
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].sessionId == sessionId)
                {
                    takeIndex = Mathf.Max(takeIndex, index.takes[i].takeIndex + 1);
                }
            }

            return takeIndex;
        }

        private static MotionTakeSummary ToSummary(MotionTakeRecord take, RecordingSessionRecord session)
        {
            return new MotionTakeSummary
            {
                id = take.id,
                sessionId = take.sessionId,
                createdAtTicks = take.createdAtTicks,
                clipName = take.clipName,
                takeIndex = take.takeIndex,
                captureMode = take.captureMode,
                durationSeconds = take.durationSeconds,
                frameCount = take.frameCount,
                nominalFrameRate = take.nominalFrameRate,
                barLength = take.barLength,
                beatLength = take.beatLength,
                startBeatOffset = take.startBeatOffset,
                isAccepted = take.isAccepted,
                localFilePath = take.localFilePath,
                uploadStatus = take.uploadStatus,
                remoteObjectKey = take.remoteObjectKey,
                schemaVersion = take.schemaVersion,
                tempoSourceType = session.tempoSourceType,
                audioAssetReference = session.audioAssetReference,
                bpm = session.bpm,
                timeSignatureNumerator = session.timeSignatureNumerator,
                timeSignatureDenominator = session.timeSignatureDenominator,
                countInBarCount = session.countInBarCount,
            };
        }

        private ArchiveIndex LoadIndex()
        {
            if (!File.Exists(indexPath))
            {
                return new ArchiveIndex();
            }

            try
            {
                return JsonUtility.FromJson<ArchiveIndex>(File.ReadAllText(indexPath)) ?? new ArchiveIndex();
            }
            catch
            {
                return new ArchiveIndex();
            }
        }

        private void SaveIndex(ArchiveIndex index)
        {
            File.WriteAllText(indexPath, JsonUtility.ToJson(index, true));
        }

        private static RecordingSessionRecord FindSession(ArchiveIndex index, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            for (var i = 0; i < index.sessions.Count; i += 1)
            {
                if (index.sessions[i].id == sessionId)
                {
                    return index.sessions[i];
                }
            }

            return null;
        }
    }
}
