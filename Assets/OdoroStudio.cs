using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Odoro
{
    public sealed class OdoroStudioRuntime : MonoBehaviour
    {
        private readonly MotionRecordingContext defaultRecordingContext = MotionRecordingContext.DefaultMetronomeLoop;

        private MotionArchiveStore archiveStore;
        private MockMotionSource motionSource;
        private MotionStudioInteractor interactor;
        private SkeletonView skeletonView;

        private MotionRecordingContext recordingContext;
        private MotionFrame latestPreviewFrame;
        private MotionClip selectedClip;
        private MotionTakeSummary selectedTake;
        private StudioScreen screen = StudioScreen.Capture;
        private Vector2 libraryScrollPosition;
        private List<MotionTakeSummary> libraryClips = new List<MotionTakeSummary>();
        private List<MotionTakeSummary> currentSessionTakes = new List<MotionTakeSummary>();
        private string currentSessionId;
        private string transientMessage;
        private float transientMessageExpiresAt;
        private float playbackTime;
        private bool stageLoopPlayback = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Object.FindFirstObjectByType<OdoroStudioRuntime>() != null)
            {
                return;
            }

            var runtime = new GameObject("Odoro Studio Runtime");
            runtime.AddComponent<OdoroStudioRuntime>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            recordingContext = defaultRecordingContext;
            archiveStore = new MotionArchiveStore();
            motionSource = new MockMotionSource();
            interactor = new MotionStudioInteractor(motionSource, recordingContext.FixedCaptureDuration);
            skeletonView = new SkeletonView("Odoro Skeleton View");

            ConfigureCamera();
            ConfigureInteractor();
            RefreshLibrary();
            motionSource.Activate(MotionSourceActivity.Preview);
        }

        private void OnDestroy()
        {
            motionSource?.Deactivate();
            skeletonView?.Dispose();
        }

        private void Update()
        {
            motionSource.Tick(Time.unscaledTime);

            if (screen == StudioScreen.Stage && selectedClip != null)
            {
                if (interactor.State.IsPlaying)
                {
                    playbackTime += Time.unscaledDeltaTime;
                    if (selectedClip.Duration > 0f && playbackTime > selectedClip.Duration)
                    {
                        playbackTime = stageLoopPlayback ? playbackTime % selectedClip.Duration : selectedClip.Duration;
                        if (!stageLoopPlayback)
                        {
                            interactor.SetPlaybackActive(false);
                        }
                    }
                }

                skeletonView.SetFrame(selectedClip.Sample(playbackTime), Palette.StageSkeleton);
            }
            else
            {
                skeletonView.SetFrame(latestPreviewFrame, Palette.CaptureSkeleton);
            }

            if (!string.IsNullOrEmpty(transientMessage) && Time.unscaledTime > transientMessageExpiresAt)
            {
                transientMessage = null;
            }
        }

        private void OnGUI()
        {
            ConfigureGuiSkin();

            var width = Mathf.Min(Screen.width - 48f, 460f);
            var height = Screen.height - 48f;
            var area = new Rect(24f, 24f, width, height);

            GUILayout.BeginArea(area, GUIContent.none, GuiStyles.Window);
            DrawHeader();

            switch (screen)
            {
                case StudioScreen.Capture:
                    DrawCaptureScreen();
                    break;
                case StudioScreen.SessionSettings:
                    DrawSessionSettingsScreen();
                    break;
                case StudioScreen.ClipsLibrary:
                    DrawLibraryScreen();
                    break;
                case StudioScreen.Stage:
                    DrawStageScreen();
                    break;
            }

            GUILayout.FlexibleSpace();
            DrawFooter();
            GUILayout.EndArea();
        }

        private void ConfigureCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
            }

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Palette.Background;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 1.25f;
            mainCamera.transform.position = new Vector3(0f, 0.95f, -10f);
            mainCamera.transform.rotation = Quaternion.identity;
        }

        private void ConfigureInteractor()
        {
            motionSource.OnFrame += frame => { latestPreviewFrame = frame; };
            motionSource.OnStatusTextChanged += interactor.SetStatusText;

            interactor.OnStateChanged += _ => { };
            interactor.OnClipChanged += clip =>
            {
                if (clip == null)
                {
                    selectedClip = null;
                    selectedTake = null;
                    playbackTime = 0f;
                    return;
                }

                selectedClip = clip;
                playbackTime = 0f;
            };

            interactor.OnRecordingCompleted += HandleRecordingCompleted;
        }

        private void HandleRecordingCompleted(MotionClip clip)
        {
            motionSource.Activate(MotionSourceActivity.Preview);

            try
            {
                selectedTake = archiveStore.SaveTake(
                    clip,
                    CaptureMode.Mock,
                    recordingContext,
                    currentSessionId
                );
                currentSessionId = selectedTake.SessionId;
                selectedClip = clip;
                playbackTime = 0f;
                interactor.SetPlaybackActive(false);
                RefreshLibrary();
                screen = StudioScreen.Stage;
                ShowTransientMessage("新しいテイクを保存しました。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowTransientMessage("保存に失敗しました。Console を確認してください。");
            }
        }

        private void RefreshLibrary()
        {
            libraryClips = archiveStore.FetchAllTakeSummaries();
            currentSessionTakes = string.IsNullOrEmpty(currentSessionId)
                ? new List<MotionTakeSummary>()
                : archiveStore.FetchTakeSummariesInSession(currentSessionId);
        }

        private void DrawHeader()
        {
            GUILayout.Label("Odoro Studio", GuiStyles.Title);
            GUILayout.Label(StatusSummary(), GuiStyles.Subtitle);

            if (!string.IsNullOrEmpty(transientMessage))
            {
                var previousColor = GUI.color;
                GUI.color = Palette.Toast;
                GUILayout.Label(transientMessage, GuiStyles.Toast);
                GUI.color = previousColor;
            }

            GUILayout.Space(12f);
        }

        private void DrawCaptureScreen()
        {
            GUILayout.Label("Capture", GuiStyles.SectionTitle);
            GUILayout.Label(
                "odoro-native の Studio フローを Unity に移した最初の土台です。今は mock モーションで、録画から保存・再生まで通せます。",
                GuiStyles.Body
            );

            DrawCard(() =>
            {
                GUILayout.Label("Capture Mode", GuiStyles.CardLabel);
                GUILayout.Label("Mock Full Body", GuiStyles.CardValue);
                GUILayout.Space(8f);
                GUILayout.Label("Recording Context", GuiStyles.CardLabel);
                GUILayout.Label(RecordingContextSummary(), GuiStyles.CardValue);
            });

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUI.enabled = !interactor.State.IsRecording;
            if (GUILayout.Button("Start Recording", GuiStyles.PrimaryButton, GUILayout.Height(40f)))
            {
                StartRecording();
            }

            GUI.enabled = interactor.State.IsRecording;
            if (GUILayout.Button("Stop", GuiStyles.SecondaryButton, GUILayout.Height(40f)))
            {
                StopRecording();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUI.enabled = !interactor.State.IsRecording;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Session Settings", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                screen = StudioScreen.SessionSettings;
            }

            if (GUILayout.Button("Clip Library", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                RefreshLibrary();
                screen = StudioScreen.ClipsLibrary;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = selectedClip != null;
            if (GUILayout.Button("Open Stage", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                screen = StudioScreen.Stage;
            }
            GUI.enabled = true;

            GUILayout.Space(12f);
            DrawCard(() =>
            {
                GUILayout.Label("Current Session", GuiStyles.CardLabel);
                GUILayout.Label($"{currentSessionTakes.Count} takes", GuiStyles.CardValue);
                GUILayout.Label("All Saved Takes", GuiStyles.CardLabel);
                GUILayout.Label($"{libraryClips.Count} takes", GuiStyles.CardValue);
            });
        }

        private void DrawSessionSettingsScreen()
        {
            GUILayout.Label("Session Settings", GuiStyles.SectionTitle);
            GUILayout.Label("ネイティブ版の `MotionRecordingContext` を Unity に持ち込み、2 小節固定キャプチャを維持しています。", GuiStyles.Body);

            DrawCard(() =>
            {
                DrawStepper("BPM", recordingContext.Bpm.ToString("0"), () => AdjustBpm(-5f), () => AdjustBpm(5f));
                DrawStepper("Numerator", recordingContext.TimeSignatureNumerator.ToString(), () => AdjustNumerator(-1), () => AdjustNumerator(1));
                DrawStepper("Denominator", recordingContext.TimeSignatureDenominator.ToString(), () => AdjustDenominator(-1), () => AdjustDenominator(1));
                DrawStepper("Count In Bars", recordingContext.CountInBarCount.ToString(), () => AdjustCountInBars(-1), () => AdjustCountInBars(1));

                GUILayout.Space(8f);
                GUILayout.Label("Fixed Capture Duration", GuiStyles.CardLabel);
                GUILayout.Label($"{recordingContext.FixedCaptureDuration:0.00} sec", GuiStyles.CardValue);
            });

            GUILayout.Space(12f);
            if (GUILayout.Button("Back to Capture", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                screen = StudioScreen.Capture;
            }
        }

        private void DrawLibraryScreen()
        {
            GUILayout.Label("Clip Library", GuiStyles.SectionTitle);
            GUILayout.Label("保存済みテイクを読み戻して、すぐに Stage で確認できます。", GuiStyles.Body);

            if (libraryClips.Count == 0)
            {
                DrawCard(() => { GUILayout.Label("まだ保存されたテイクはありません。", GuiStyles.CardValue); });
            }
            else
            {
                libraryScrollPosition = GUILayout.BeginScrollView(libraryScrollPosition, false, true, GUILayout.Height(300f));
                foreach (var take in libraryClips)
                {
                    DrawCard(() =>
                    {
                        GUILayout.Label(take.DisplayName, GuiStyles.CardValue);
                        GUILayout.Label(take.SecondarySummary, GuiStyles.CardLabel);
                        if (GUILayout.Button("Load in Stage", GuiStyles.SecondaryButton, GUILayout.Height(32f)))
                        {
                            LoadTake(take);
                        }
                    });
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("Back to Capture", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                screen = StudioScreen.Capture;
            }
        }

        private void DrawStageScreen()
        {
            GUILayout.Label("Stage", GuiStyles.SectionTitle);
            GUILayout.Label("ステージ再生は native 側の `stage` 画面の最小移植版です。今は canonical skeleton をそのままプレビューします。", GuiStyles.Body);

            DrawCard(() =>
            {
                GUILayout.Label("Selected Take", GuiStyles.CardLabel);
                GUILayout.Label(selectedTake != null ? selectedTake.DisplayName : "Unsaved Preview", GuiStyles.CardValue);
                GUILayout.Label("Playback", GuiStyles.CardLabel);
                GUILayout.Label(selectedClip != null ? $"{selectedClip.Duration:0.00} sec / {selectedClip.FrameCount} frames" : "No clip loaded", GuiStyles.CardValue);
            });

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUI.enabled = selectedClip != null;
            if (GUILayout.Button(interactor.State.IsPlaying ? "Pause" : "Play", GuiStyles.PrimaryButton, GUILayout.Height(40f)))
            {
                TogglePlayback();
            }

            if (GUILayout.Button("Restart", GuiStyles.SecondaryButton, GUILayout.Height(40f)))
            {
                playbackTime = 0f;
                interactor.SetPlaybackActive(false);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clip Library", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                RefreshLibrary();
                screen = StudioScreen.ClipsLibrary;
            }

            if (GUILayout.Button("Back to Capture", GuiStyles.SecondaryButton, GUILayout.Height(36f)))
            {
                interactor.SetPlaybackActive(false);
                screen = StudioScreen.Capture;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.Space(12f);
            GUILayout.Label("Saved clips are written to Application.persistentDataPath as JSON payloads for now.", GuiStyles.Footnote);
        }

        private string StatusSummary()
        {
            var state = interactor.State;
            if (state.IsRecording)
            {
                return $"Recording {state.RecordingDuration:0.00}s / {recordingContext.FixedCaptureDuration:0.00}s";
            }

            if (screen == StudioScreen.Stage && selectedClip != null)
            {
                return interactor.State.IsPlaying
                    ? $"Playing {playbackTime:0.00}s / {selectedClip.Duration:0.00}s"
                    : $"Stage ready: {selectedClip.FrameCount} frames";
            }

            return state.StatusText;
        }

        private string RecordingContextSummary()
        {
            return $"{recordingContext.Bpm:0} BPM  •  {recordingContext.TimeSignatureNumerator}/{recordingContext.TimeSignatureDenominator}  •  2 bars fixed";
        }

        private void StartRecording()
        {
            motionSource.Activate(MotionSourceActivity.Recording);
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
            interactor.BeginRecording();
            ShowTransientMessage("録画を開始しました。");
        }

        private void StopRecording()
        {
            interactor.StopRecording();
            motionSource.Activate(MotionSourceActivity.Preview);
        }

        private void LoadTake(MotionTakeSummary take)
        {
            selectedTake = take;
            currentSessionId = take.SessionId;
            currentSessionTakes = archiveStore.FetchTakeSummariesInSession(currentSessionId);
            selectedClip = archiveStore.LoadClip(take.TakeId);
            playbackTime = 0f;
            interactor.ReplaceCurrentClip(selectedClip);
            interactor.SetPlaybackActive(false);
            screen = StudioScreen.Stage;
            ShowTransientMessage("保存済みテイクを読み込みました。");
        }

        private void TogglePlayback()
        {
            if (selectedClip == null)
            {
                return;
            }

            if (!interactor.State.IsPlaying && playbackTime >= selectedClip.Duration)
            {
                playbackTime = 0f;
            }

            interactor.SetPlaybackActive(!interactor.State.IsPlaying);
        }

        private void AdjustBpm(float delta)
        {
            recordingContext.Bpm = Mathf.Clamp(recordingContext.Bpm + delta, 60f, 200f);
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustNumerator(int delta)
        {
            recordingContext.TimeSignatureNumerator = Mathf.Clamp(recordingContext.TimeSignatureNumerator + delta, 2, 7);
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustDenominator(int delta)
        {
            var options = new[] { 2, 4, 8 };
            var currentIndex = Array.IndexOf(options, recordingContext.TimeSignatureDenominator);
            var nextIndex = Mathf.Clamp(currentIndex + delta, 0, options.Length - 1);
            recordingContext.TimeSignatureDenominator = options[nextIndex];
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustCountInBars(int delta)
        {
            recordingContext.CountInBarCount = Mathf.Clamp(recordingContext.CountInBarCount + delta, 0, 4);
        }

        private void ShowTransientMessage(string message)
        {
            transientMessage = message;
            transientMessageExpiresAt = Time.unscaledTime + 2.5f;
        }

        private void DrawStepper(string label, string value, Action decrease, Action increase)
        {
            GUILayout.Label(label, GuiStyles.CardLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GuiStyles.SmallButton, GUILayout.Width(44f), GUILayout.Height(32f)))
            {
                decrease();
            }

            GUILayout.Label(value, GuiStyles.StepperValue, GUILayout.Width(120f));

            if (GUILayout.Button("+", GuiStyles.SmallButton, GUILayout.Width(44f), GUILayout.Height(32f)))
            {
                increase();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawCard(Action content)
        {
            GUILayout.BeginVertical(GuiStyles.Card);
            content();
            GUILayout.EndVertical();
        }

        private static void ConfigureGuiSkin()
        {
            GUI.skin.label.richText = true;
            GUI.skin.button.richText = true;
        }
    }

    internal enum StudioScreen
    {
        Capture,
        SessionSettings,
        ClipsLibrary,
        Stage,
    }

    internal enum MotionSourceActivity
    {
        Preview,
        Recording,
    }

    internal enum CaptureMode
    {
        Mock,
    }

    internal enum OdoroJoint
    {
        Root = 0,
        Head = 1,
        Nose = 2,
        LeftShoulder = 3,
        RightShoulder = 4,
        LeftElbow = 5,
        RightElbow = 6,
        LeftWrist = 7,
        RightWrist = 8,
        LeftHip = 9,
        RightHip = 10,
        LeftKnee = 11,
        RightKnee = 12,
        LeftAnkle = 13,
        RightAnkle = 14,
        LeftFoot = 15,
        RightFoot = 16,
    }

    [Serializable]
    internal struct MotionRecordingContext
    {
        public const int FixedCaptureBarCount = 2;

        public float Bpm;
        public int TimeSignatureNumerator;
        public int TimeSignatureDenominator;
        public int CountInBarCount;

        public float FixedCaptureDuration
        {
            get
            {
                if (Bpm <= 0f)
                {
                    return 0f;
                }

                return FixedCaptureBarCount * TimeSignatureNumerator * 60f / Bpm;
            }
        }

        public static MotionRecordingContext DefaultMetronomeLoop => new MotionRecordingContext
        {
            Bpm = 120f,
            TimeSignatureNumerator = 4,
            TimeSignatureDenominator = 4,
            CountInBarCount = 1,
        };
    }

    [Serializable]
    internal sealed class MotionFrame
    {
        public float time;
        public Vector3[] joints;

        public MotionFrame CloneWithTime(float newTime)
        {
            var clone = new Vector3[joints.Length];
            Array.Copy(joints, clone, joints.Length);
            return new MotionFrame { time = newTime, joints = clone };
        }
    }

    [Serializable]
    internal sealed class MotionClip
    {
        public List<MotionFrame> frames = new List<MotionFrame>();

        public int FrameCount => frames.Count;
        public float Duration => FrameCount == 0 ? 0f : frames[FrameCount - 1].time;
        public bool IsEmpty => FrameCount == 0;

        public MotionFrame Sample(float time)
        {
            if (FrameCount == 0)
            {
                return null;
            }

            if (time <= frames[0].time || FrameCount == 1)
            {
                return frames[0];
            }

            for (var index = 1; index < FrameCount; index += 1)
            {
                var next = frames[index];
                if (time > next.time)
                {
                    continue;
                }

                var previous = frames[index - 1];
                var span = Mathf.Max(0.0001f, next.time - previous.time);
                var blend = Mathf.Clamp01((time - previous.time) / span);
                return Interpolate(previous, next, blend);
            }

            return frames[FrameCount - 1];
        }

        private static MotionFrame Interpolate(MotionFrame from, MotionFrame to, float blend)
        {
            var jointCount = Mathf.Min(from.joints.Length, to.joints.Length);
            var joints = new Vector3[jointCount];
            for (var index = 0; index < jointCount; index += 1)
            {
                joints[index] = Vector3.Lerp(from.joints[index], to.joints[index], blend);
            }

            return new MotionFrame
            {
                time = Mathf.Lerp(from.time, to.time, blend),
                joints = joints,
            };
        }
    }

    internal sealed class MotionStudioInteractor
    {
        public MotionStudioState State { get; private set; } = MotionStudioState.CreateDefault();
        public MotionClip CurrentClip { get; private set; }

        public event Action<MotionStudioState> OnStateChanged;
        public event Action<MotionClip> OnClipChanged;
        public event Action<MotionClip> OnRecordingCompleted;

        private readonly MockMotionSource source;
        private readonly List<MotionFrame> capturedFrames = new List<MotionFrame>();
        private float firstFrameTimestamp = -1f;
        private float maximumCaptureDuration;

        public MotionStudioInteractor(MockMotionSource source, float maximumCaptureDuration)
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
            State.IsRecording = true;
            State.RecordedFrameCount = 0;
            State.RecordingDuration = 0f;
            State.StatusText = "モーションを記録しています。";
            PublishState();
        }

        public void StopRecording()
        {
            if (!State.IsRecording)
            {
                return;
            }

            State.IsRecording = false;

            if (capturedFrames.Count < 2)
            {
                State.StatusText = "記録フレームが足りませんでした。";
                PublishState();
                return;
            }

            var clip = new MotionClip();
            clip.frames.AddRange(capturedFrames);
            ReplaceCurrentClip(clip);
            State.StatusText = "キャプチャが完了しました。";
            PublishState();
            OnRecordingCompleted?.Invoke(clip);
        }

        public void ReplaceCurrentClip(MotionClip clip)
        {
            CurrentClip = clip;
            State.HasClip = clip != null && !clip.IsEmpty;
            State.ClipDuration = clip?.Duration ?? 0f;
            OnClipChanged?.Invoke(CurrentClip);
            PublishState();
        }

        public void SetPlaybackActive(bool isPlaying)
        {
            State.IsPlaying = isPlaying;
            PublishState();
        }

        public void SetStatusText(string statusText)
        {
            if (State.IsRecording)
            {
                return;
            }

            State.StatusText = statusText;
            PublishState();
        }

        private void Consume(MotionFrame frame)
        {
            if (!State.IsRecording)
            {
                return;
            }

            if (firstFrameTimestamp < 0f)
            {
                firstFrameTimestamp = frame.time;
            }

            var relativeTime = Mathf.Max(0f, frame.time - firstFrameTimestamp);
            capturedFrames.Add(frame.CloneWithTime(relativeTime));
            State.RecordedFrameCount = capturedFrames.Count;
            State.RecordingDuration = Mathf.Min(maximumCaptureDuration, relativeTime);
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

    internal sealed class MotionStudioState
    {
        public string StatusText;
        public bool IsRecording;
        public bool IsPlaying;
        public int RecordedFrameCount;
        public float RecordingDuration;
        public float ClipDuration;
        public bool HasClip;

        public static MotionStudioState CreateDefault()
        {
            return new MotionStudioState
            {
                StatusText = "プレビュー待機中です。",
                IsRecording = false,
                IsPlaying = false,
                RecordedFrameCount = 0,
                RecordingDuration = 0f,
                ClipDuration = 0f,
                HasClip = false,
            };
        }
    }

    internal sealed class MockMotionSource
    {
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
            var elapsed = Mathf.Max(0f, now - startTime);
            OnFrame?.Invoke(MakeFrame(elapsed));
        }

        private MotionFrame MakeFrame(float time)
        {
            var rhythm = time;
            var step = Mathf.Sin(rhythm * 2.3f);
            var sway = Mathf.Sin(rhythm * 1.4f);
            var armSwing = Mathf.Sin(rhythm * 3.1f);
            var bounce = Mathf.Max(0f, Mathf.Sin(rhythm * 4.2f)) * 0.08f;
            var activityBias = currentActivity == MotionSourceActivity.Recording ? 1f : 0.8f;

            var root = new Vector3(sway * 0.18f, 0.92f + bounce, 0f);
            var head = root + new Vector3(0f, 0.60f, 0f);
            var nose = head + new Vector3(0f, 0.04f, 0.08f);
            var leftShoulder = root + new Vector3(-0.22f, 0.42f, 0f);
            var rightShoulder = root + new Vector3(0.22f, 0.42f, 0f);
            var leftElbow = leftShoulder + new Vector3(-0.18f, 0.04f + armSwing * 0.14f * activityBias, 0f);
            var rightElbow = rightShoulder + new Vector3(0.18f, 0.04f - armSwing * 0.14f * activityBias, 0f);
            var leftWrist = leftElbow + new Vector3(-0.16f, -0.10f + armSwing * 0.10f * activityBias, 0f);
            var rightWrist = rightElbow + new Vector3(0.16f, -0.10f - armSwing * 0.10f * activityBias, 0f);
            var leftHip = root + new Vector3(-0.12f, -0.02f, 0f);
            var rightHip = root + new Vector3(0.12f, -0.02f, 0f);
            var leftKnee = leftHip + new Vector3(-0.02f, -0.34f + Mathf.Max(0f, step) * 0.10f, 0f);
            var rightKnee = rightHip + new Vector3(0.02f, -0.34f + Mathf.Max(0f, -step) * 0.10f, 0f);
            var leftAnkle = leftKnee + new Vector3(0f, -0.33f, 0f);
            var rightAnkle = rightKnee + new Vector3(0f, -0.33f, 0f);
            var leftFoot = leftAnkle + new Vector3(0.03f, -0.02f, 0.08f);
            var rightFoot = rightAnkle + new Vector3(0.03f, -0.02f, 0.08f);

            return new MotionFrame
            {
                time = time,
                joints = new[]
                {
                    root,
                    head,
                    nose,
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
                },
            };
        }
    }

    [Serializable]
    internal sealed class MotionTakeSummary
    {
        public string TakeId;
        public string SessionId;
        public long CreatedAtTicks;
        public string ClipName;
        public int TakeIndex;
        public string CaptureMode;
        public float DurationSeconds;
        public int FrameCount;
        public float Bpm;
        public int TimeSignatureNumerator;
        public int TimeSignatureDenominator;
        public string PayloadFilename;

        public string DisplayName => string.IsNullOrEmpty(ClipName) ? $"Take {TakeIndex:00}" : ClipName;

        public string SecondarySummary
        {
            get
            {
                var createdAt = new DateTime(CreatedAtTicks, DateTimeKind.Utc).ToLocalTime();
                return $"{createdAt:MM/dd HH:mm}  •  {DurationSeconds:0.00}s  •  {FrameCount} frames  •  {Bpm:0} BPM";
            }
        }
    }

    [Serializable]
    internal sealed class RecordingSessionRecord
    {
        public string SessionId;
        public long CreatedAtTicks;
        public float Bpm;
        public int TimeSignatureNumerator;
        public int TimeSignatureDenominator;
        public int CountInBarCount;
    }

    [Serializable]
    internal sealed class ArchiveIndex
    {
        public List<RecordingSessionRecord> sessions = new List<RecordingSessionRecord>();
        public List<MotionTakeSummary> takes = new List<MotionTakeSummary>();
    }

    internal sealed class MotionArchiveStore
    {
        private readonly string rootPath;
        private readonly string clipsPath;
        private readonly string indexPath;

        public MotionArchiveStore()
        {
            rootPath = Path.Combine(Application.persistentDataPath, "OdoroArchive");
            clipsPath = Path.Combine(rootPath, "Clips");
            indexPath = Path.Combine(rootPath, "archive-index.json");

            Directory.CreateDirectory(rootPath);
            Directory.CreateDirectory(clipsPath);
        }

        public MotionTakeSummary SaveTake(MotionClip clip, CaptureMode captureMode, MotionRecordingContext context, string existingSessionId)
        {
            var index = LoadIndex();
            var session = ResolveSession(index, context, existingSessionId);
            var takeIndex = 1;
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].SessionId == session.SessionId)
                {
                    takeIndex = Mathf.Max(takeIndex, index.takes[i].TakeIndex + 1);
                }
            }

            var takeId = Guid.NewGuid().ToString("N");
            var payloadFilename = $"{takeId}.json";
            var payloadPath = Path.Combine(clipsPath, payloadFilename);
            File.WriteAllText(payloadPath, JsonUtility.ToJson(clip, true));

            var summary = new MotionTakeSummary
            {
                TakeId = takeId,
                SessionId = session.SessionId,
                CreatedAtTicks = DateTime.UtcNow.Ticks,
                ClipName = $"Take {takeIndex:00}",
                TakeIndex = takeIndex,
                CaptureMode = captureMode.ToString(),
                DurationSeconds = clip.Duration,
                FrameCount = clip.FrameCount,
                Bpm = context.Bpm,
                TimeSignatureNumerator = context.TimeSignatureNumerator,
                TimeSignatureDenominator = context.TimeSignatureDenominator,
                PayloadFilename = payloadFilename,
            };

            index.takes.Add(summary);
            SaveIndex(index);
            return summary;
        }

        public MotionClip LoadClip(string takeId)
        {
            var index = LoadIndex();
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].TakeId != takeId)
                {
                    continue;
                }

                var path = Path.Combine(clipsPath, index.takes[i].PayloadFilename);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Clip payload not found: {path}");
                }

                return JsonUtility.FromJson<MotionClip>(File.ReadAllText(path));
            }

            throw new FileNotFoundException($"Take not found: {takeId}");
        }

        public List<MotionTakeSummary> FetchAllTakeSummaries()
        {
            var index = LoadIndex();
            index.takes.Sort((left, right) => right.CreatedAtTicks.CompareTo(left.CreatedAtTicks));
            return new List<MotionTakeSummary>(index.takes);
        }

        public List<MotionTakeSummary> FetchTakeSummariesInSession(string sessionId)
        {
            var index = LoadIndex();
            var takes = new List<MotionTakeSummary>();
            for (var i = 0; i < index.takes.Count; i += 1)
            {
                if (index.takes[i].SessionId == sessionId)
                {
                    takes.Add(index.takes[i]);
                }
            }

            takes.Sort((left, right) => left.TakeIndex.CompareTo(right.TakeIndex));
            return takes;
        }

        private RecordingSessionRecord ResolveSession(ArchiveIndex index, MotionRecordingContext context, string existingSessionId)
        {
            if (!string.IsNullOrEmpty(existingSessionId))
            {
                for (var i = 0; i < index.sessions.Count; i += 1)
                {
                    if (index.sessions[i].SessionId == existingSessionId)
                    {
                        return index.sessions[i];
                    }
                }
            }

            var session = new RecordingSessionRecord
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CreatedAtTicks = DateTime.UtcNow.Ticks,
                Bpm = context.Bpm,
                TimeSignatureNumerator = context.TimeSignatureNumerator,
                TimeSignatureDenominator = context.TimeSignatureDenominator,
                CountInBarCount = context.CountInBarCount,
            };
            index.sessions.Add(session);
            return session;
        }

        private ArchiveIndex LoadIndex()
        {
            if (!File.Exists(indexPath))
            {
                return new ArchiveIndex();
            }

            return JsonUtility.FromJson<ArchiveIndex>(File.ReadAllText(indexPath)) ?? new ArchiveIndex();
        }

        private void SaveIndex(ArchiveIndex index)
        {
            File.WriteAllText(indexPath, JsonUtility.ToJson(index, true));
        }
    }

    internal sealed class SkeletonView
    {
        private static readonly (OdoroJoint, OdoroJoint)[] Bones =
        {
            (OdoroJoint.Root, OdoroJoint.Head),
            (OdoroJoint.Head, OdoroJoint.Nose),
            (OdoroJoint.Head, OdoroJoint.LeftShoulder),
            (OdoroJoint.Head, OdoroJoint.RightShoulder),
            (OdoroJoint.LeftShoulder, OdoroJoint.LeftElbow),
            (OdoroJoint.LeftElbow, OdoroJoint.LeftWrist),
            (OdoroJoint.RightShoulder, OdoroJoint.RightElbow),
            (OdoroJoint.RightElbow, OdoroJoint.RightWrist),
            (OdoroJoint.Root, OdoroJoint.LeftHip),
            (OdoroJoint.Root, OdoroJoint.RightHip),
            (OdoroJoint.LeftHip, OdoroJoint.LeftKnee),
            (OdoroJoint.LeftKnee, OdoroJoint.LeftAnkle),
            (OdoroJoint.LeftAnkle, OdoroJoint.LeftFoot),
            (OdoroJoint.RightHip, OdoroJoint.RightKnee),
            (OdoroJoint.RightKnee, OdoroJoint.RightAnkle),
            (OdoroJoint.RightAnkle, OdoroJoint.RightFoot),
        };

        private readonly GameObject root;
        private readonly Transform[] joints = new Transform[17];
        private readonly LineRenderer[] bones = new LineRenderer[Bones.Length];
        private readonly Material lineMaterial;

        public SkeletonView(string rootName)
        {
            root = new GameObject(rootName);
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            lineMaterial = new Material(shader);

            for (var index = 0; index < joints.Length; index += 1)
            {
                var joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                joint.name = $"Joint {index:00}";
                joint.transform.SetParent(root.transform, false);
                joint.transform.localScale = Vector3.one * 0.05f;

                var collider = joint.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }

                joints[index] = joint.transform;
            }

            for (var index = 0; index < Bones.Length; index += 1)
            {
                var lineObject = new GameObject($"Bone {index:00}");
                lineObject.transform.SetParent(root.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = 0.02f;
                line.useWorldSpace = true;
                line.material = lineMaterial;
                line.numCapVertices = 8;
                bones[index] = line;
            }
        }

        public void SetFrame(MotionFrame frame, Color color)
        {
            root.SetActive(frame != null);
            if (frame == null || frame.joints == null || frame.joints.Length < joints.Length)
            {
                return;
            }

            for (var index = 0; index < joints.Length; index += 1)
            {
                joints[index].position = frame.joints[index];
                var renderer = joints[index].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }

            for (var index = 0; index < Bones.Length; index += 1)
            {
                var start = (int)Bones[index].Item1;
                var end = (int)Bones[index].Item2;
                bones[index].startColor = color;
                bones[index].endColor = color;
                bones[index].SetPosition(0, frame.joints[start]);
                bones[index].SetPosition(1, frame.joints[end]);
            }
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }

            if (lineMaterial != null)
            {
                UnityEngine.Object.Destroy(lineMaterial);
            }
        }
    }

    internal static class Palette
    {
        public static readonly Color Background = new Color(0.05f, 0.07f, 0.11f);
        public static readonly Color CaptureSkeleton = new Color(0.35f, 0.87f, 0.95f);
        public static readonly Color StageSkeleton = new Color(1.0f, 0.62f, 0.42f);
        public static readonly Color Toast = new Color(0.18f, 0.55f, 0.36f);
    }

    internal static class GuiStyles
    {
        private static GUIStyle title;
        private static GUIStyle subtitle;
        private static GUIStyle sectionTitle;
        private static GUIStyle body;
        private static GUIStyle card;
        private static GUIStyle cardLabel;
        private static GUIStyle cardValue;
        private static GUIStyle primaryButton;
        private static GUIStyle secondaryButton;
        private static GUIStyle smallButton;
        private static GUIStyle stepperValue;
        private static GUIStyle footnote;
        private static GUIStyle window;
        private static GUIStyle toast;

        public static GUIStyle Title => title ??= BuildLabel(26, FontStyle.Bold, Color.white);
        public static GUIStyle Subtitle => subtitle ??= BuildLabel(12, FontStyle.Normal, new Color(0.74f, 0.80f, 0.86f));
        public static GUIStyle SectionTitle => sectionTitle ??= BuildLabel(20, FontStyle.Bold, Color.white);
        public static GUIStyle Body => body ??= BuildLabel(13, FontStyle.Normal, new Color(0.86f, 0.89f, 0.92f));
        public static GUIStyle Card => card ??= BuildCard();
        public static GUIStyle CardLabel => cardLabel ??= BuildLabel(12, FontStyle.Normal, new Color(0.72f, 0.78f, 0.84f));
        public static GUIStyle CardValue => cardValue ??= BuildLabel(16, FontStyle.Bold, Color.white);
        public static GUIStyle PrimaryButton => primaryButton ??= BuildButton(new Color(0.18f, 0.52f, 0.87f), Color.white);
        public static GUIStyle SecondaryButton => secondaryButton ??= BuildButton(new Color(0.16f, 0.19f, 0.24f), Color.white);
        public static GUIStyle SmallButton => smallButton ??= BuildButton(new Color(0.16f, 0.19f, 0.24f), Color.white, 16);
        public static GUIStyle StepperValue => stepperValue ??= BuildLabel(18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        public static GUIStyle Footnote => footnote ??= BuildLabel(11, FontStyle.Normal, new Color(0.61f, 0.67f, 0.73f));
        public static GUIStyle Window => window ??= BuildWindow();
        public static GUIStyle Toast => toast ??= BuildToast();

        private static GUIStyle BuildLabel(int fontSize, FontStyle fontStyle, Color textColor, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                wordWrap = true,
                alignment = alignment,
                richText = true,
                normal = { textColor = textColor },
                margin = new RectOffset(0, 0, 2, 2),
            };
        }

        private static GUIStyle BuildButton(Color backgroundColor, Color textColor, int fontSize = 14)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0,
                padding = new RectOffset(16, 16, 10, 10),
                richText = true,
            };

            style.normal.background = MakeTexture(backgroundColor);
            style.hover.background = MakeTexture(backgroundColor * 1.1f);
            style.active.background = MakeTexture(backgroundColor * 0.9f);
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            return style;
        }

        private static GUIStyle BuildCard()
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.11f, 0.14f, 0.19f, 0.94f)) },
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 6, 6),
            };
        }

        private static GUIStyle BuildWindow()
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.08f, 0.10f, 0.14f, 0.92f)) },
                padding = new RectOffset(20, 20, 20, 20),
            };
        }

        private static GUIStyle BuildToast()
        {
            return new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 8, 8),
                normal =
                {
                    background = MakeTexture(new Color(0.14f, 0.36f, 0.24f)),
                    textColor = Color.white,
                },
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
