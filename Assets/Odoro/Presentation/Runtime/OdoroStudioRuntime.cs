using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class OdoroStudioRuntime : MonoBehaviour
    {
        private MotionArchiveStore archiveStore;
        private IMotionSource motionSource;
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

            recordingContext = MotionRecordingContext.DefaultMetronomeLoop.NormalizedForFixedCaptureLength();
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
                if (interactor.State.isPlaying)
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
            GuiStyles.ConfigureGuiSkin();

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

            interactor.OnClipChanged += clip =>
            {
                selectedClip = clip;
                playbackTime = 0f;
                if (clip == null)
                {
                    selectedTake = null;
                }
            };

            interactor.OnRecordingCompleted += HandleRecordingCompleted;
        }

        private void HandleRecordingCompleted(MotionClip clip)
        {
            motionSource.Activate(MotionSourceActivity.Preview);

            try
            {
                selectedTake = archiveStore.SaveTake(
                    interactor.SourceClip ?? clip,
                    motionSource.CaptureMode,
                    recordingContext,
                    currentSessionId
                );
                currentSessionId = selectedTake.sessionId;
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
                "Domain / Application / Infrastructure / Presentation に分け、保存モデルは native 側と同じ発想へ寄せています。",
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
            GUI.enabled = !interactor.State.isRecording;
            if (GUILayout.Button("Start Recording", GuiStyles.PrimaryButton, GUILayout.Height(40f)))
            {
                StartRecording();
            }

            GUI.enabled = interactor.State.isRecording;
            if (GUILayout.Button("Stop", GuiStyles.SecondaryButton, GUILayout.Height(40f)))
            {
                StopRecording();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUI.enabled = !interactor.State.isRecording;
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
            GUILayout.Label("`MotionRecordingContext` は native 側と同じく 2 小節固定キャプチャ前提です。", GuiStyles.Body);

            DrawCard(() =>
            {
                DrawStepper("BPM", recordingContext.bpm.ToString("0"), () => AdjustBpm(-5f), () => AdjustBpm(5f));
                DrawStepper("Numerator", recordingContext.timeSignatureNumerator.ToString(), () => AdjustNumerator(-1), () => AdjustNumerator(1));
                DrawStepper("Denominator", recordingContext.timeSignatureDenominator.ToString(), () => AdjustDenominator(-1), () => AdjustDenominator(1));
                DrawStepper("Count In Bars", recordingContext.countInBarCount.ToString(), () => AdjustCountInBars(-1), () => AdjustCountInBars(1));

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
            GUILayout.Label("`.odoro.stage` payload を読み戻して、Stage で確認できます。", GuiStyles.Body);

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
            GUILayout.Label("再生対象は canonical skeleton です。ここに後で実アバター描画を載せ替えていきます。", GuiStyles.Body);

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
            if (GUILayout.Button(interactor.State.isPlaying ? "Pause" : "Play", GuiStyles.PrimaryButton, GUILayout.Height(40f)))
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
            GUILayout.Label("Archive root: Application.persistentDataPath/OdoroArchiveV2", GuiStyles.Footnote);
        }

        private string StatusSummary()
        {
            var state = interactor.State;
            if (state.isRecording)
            {
                return $"Recording {state.recordingDuration:0.00}s / {recordingContext.FixedCaptureDuration:0.00}s";
            }

            if (screen == StudioScreen.Stage && selectedClip != null)
            {
                return state.isPlaying
                    ? $"Playing {playbackTime:0.00}s / {selectedClip.Duration:0.00}s"
                    : $"Stage ready: {selectedClip.FrameCount} frames";
            }

            return state.statusText;
        }

        private string RecordingContextSummary()
        {
            return $"{recordingContext.bpm:0} BPM  •  {recordingContext.timeSignatureNumerator}/{recordingContext.timeSignatureDenominator}  •  {recordingContext.targetBarCount} bars";
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
            currentSessionId = take.sessionId;
            currentSessionTakes = archiveStore.FetchTakeSummariesInSession(currentSessionId);
            selectedClip = archiveStore.LoadClip(take.id);
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

            if (!interactor.State.isPlaying && playbackTime >= selectedClip.Duration)
            {
                playbackTime = 0f;
            }

            interactor.SetPlaybackActive(!interactor.State.isPlaying);
        }

        private void AdjustBpm(float delta)
        {
            recordingContext.bpm = Mathf.Clamp(recordingContext.bpm + delta, 60f, 200f);
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustNumerator(int delta)
        {
            recordingContext.timeSignatureNumerator = Mathf.Clamp(recordingContext.timeSignatureNumerator + delta, 2, 7);
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustDenominator(int delta)
        {
            var options = new[] { 2, 4, 8 };
            var currentIndex = Mathf.Max(0, Array.IndexOf(options, recordingContext.timeSignatureDenominator));
            var nextIndex = Mathf.Clamp(currentIndex + delta, 0, options.Length - 1);
            recordingContext.timeSignatureDenominator = options[nextIndex];
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
        }

        private void AdjustCountInBars(int delta)
        {
            recordingContext.countInBarCount = Mathf.Clamp(recordingContext.countInBarCount + delta, 0, 4);
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
    }
}
