using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class OdoroStudioRuntime : MonoBehaviour
    {
        private const float ReferencePhoneWidth = 390f;
        private const float ReferencePhoneHeight = 844f;
        private const float PhoneAspect = ReferencePhoneWidth / ReferencePhoneHeight;
        private const float SimulatedTopInset = 54f;
        private const float SimulatedBottomInset = 34f;

        private MotionArchiveStore archiveStore;
        private IMotionSource motionSource;
        private MotionStudioInteractor interactor;
        private SkeletonView skeletonView;
        private HumanoidAvatarView avatarView;
        private Camera mainCamera;

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
            motionSource = CreateMotionSource();
            interactor = new MotionStudioInteractor(
                motionSource,
                recordingContext.FixedCaptureDuration,
                new StudioPlaybackCapturedClipPreparer(motionSource.CaptureMode)
            );
            skeletonView = new SkeletonView("Odoro Skeleton View");
            avatarView = HumanoidAvatarView.TryCreateFromResources();

            ConfigureCamera();
            ConfigureInteractor();
            RefreshLibrary();
            motionSource.Activate(MotionSourceActivity.Preview);
        }

        private void OnDestroy()
        {
            motionSource?.Deactivate();
            skeletonView?.Dispose();
            avatarView?.Dispose();
        }

        private void Update()
        {
            UpdateCameraViewport();
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

                var sampledFrame = selectedClip.Sample(playbackTime);
                if (avatarView != null && avatarView.IsAvailable)
                {
                    avatarView.SetFrame(sampledFrame);
                    skeletonView.SetFrame(null, Palette.StageSkeleton);
                }
                else
                {
                    skeletonView.SetFrame(sampledFrame, Palette.StageSkeleton);
                }
            }
            else
            {
                avatarView?.SetVisible(false);
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

            GUI.color = Palette.OuterBackground;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var previewRect = GetPreviewRect();
            DrawPhoneFrame(previewRect);

            var previousMatrix = GUI.matrix;
            GUI.BeginGroup(previewRect);
            var scale = previewRect.width / ReferencePhoneWidth;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

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

            DrawToast();
            GUI.matrix = previousMatrix;
            GUI.EndGroup();
        }

        private void ConfigureCamera()
        {
            if (motionSource is IPrimaryCameraSource providedCameraSource && providedCameraSource.ManagesCamera)
            {
                mainCamera = providedCameraSource.PrimaryCamera;
                return;
            }

            mainCamera = Camera.main;
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

        private IMotionSource CreateMotionSource()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var arSource = gameObject.AddComponent<ArFoundationBodyMotionSource>();
            if (arSource.IsSupported)
            {
                return arSource;
            }

            Destroy(arSource);
#endif

            return new MockMotionSource();
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

        private Rect ReferenceSafeRect => new Rect(
            0f,
            SimulatedTopInset,
            ReferencePhoneWidth,
            ReferencePhoneHeight - SimulatedTopInset - SimulatedBottomInset
        );

        private void DrawCaptureScreen()
        {
            var safeRect = ReferenceSafeRect;
            var headerRect = new Rect(
                safeRect.x + 16f,
                safeRect.y + 12f,
                safeRect.width - 32f,
                138f
            );
            DrawPanelArea(headerRect, GuiStyles.GlassPanel, () =>
            {
                GUILayout.Label(CaptureProgressTitle(), GuiStyles.CardValue);
                GUILayout.Label(RecordingContextSummary(), GuiStyles.Subtitle);
                GUILayout.Space(12f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(CaptureModeLabel(), GuiStyles.Pill, GUILayout.Width(140f), GUILayout.Height(32f));
                GUILayout.FlexibleSpace();
                GUILayout.Label(interactor.State.statusText, GuiStyles.Pill, GUILayout.Width(170f), GUILayout.Height(32f));
                GUILayout.EndHorizontal();
                GUILayout.Space(12f);
                GUILayout.Label("Swipe-less Unity preview of the native capture screen.", GuiStyles.Body);
            });

            var bottomRect = new Rect(
                safeRect.x + 20f,
                safeRect.yMax - 176f,
                safeRect.width - 40f,
                156f
            );
            DrawPanelArea(bottomRect, GuiStyles.DarkPanel, () =>
            {
                GUILayout.BeginHorizontal();
                GUI.enabled = !interactor.State.isRecording;
                if (GUILayout.Button("Session", GuiStyles.SecondaryButton, GUILayout.Height(38f)))
                {
                    screen = StudioScreen.SessionSettings;
                }

                if (GUILayout.Button("Library", GuiStyles.SecondaryButton, GUILayout.Height(38f)))
                {
                    RefreshLibrary();
                    screen = StudioScreen.ClipsLibrary;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.Space(12f);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = !interactor.State.isRecording;
                if (GUILayout.Button("●", GuiStyles.RecordButton, GUILayout.Width(92f), GUILayout.Height(92f)))
                {
                    StartRecording();
                }
                GUI.enabled = interactor.State.isRecording;
                if (GUILayout.Button("■", GuiStyles.RecordStopButton, GUILayout.Width(92f), GUILayout.Height(92f)))
                {
                    StopRecording();
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10f);
                GUILayout.Label(selectedClip != null ? "Latest clip ready for stage playback" : "Tap the record button to capture a take", GuiStyles.CenteredCaption);
            });
        }

        private void DrawSessionSettingsScreen()
        {
            var safeRect = ReferenceSafeRect;
            var shellRect = new Rect(safeRect.x + 16f, safeRect.y + 10f, safeRect.width - 32f, safeRect.height - 20f);
            DrawShellScreen(shellRect, "Session Settings", "Adjust BPM, meter, and count-in for the next take.", () =>
            {
                screen = StudioScreen.Capture;
            }, () =>
            {
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
            });
        }

        private void DrawLibraryScreen()
        {
            var safeRect = ReferenceSafeRect;
            var shellRect = new Rect(safeRect.x + 16f, safeRect.y + 10f, safeRect.width - 32f, safeRect.height - 20f);
            DrawShellScreen(shellRect, "Clip Library", "Review past takes and jump back into playback.", () =>
            {
                screen = StudioScreen.Capture;
            }, () =>
            {
                if (libraryClips.Count == 0)
                {
                    DrawCard(() => { GUILayout.Label("No clips yet", GuiStyles.CardValue); });
                }
                else
                {
                    libraryScrollPosition = GUILayout.BeginScrollView(libraryScrollPosition, false, true, GUILayout.Height(shellRect.height - 170f));
                    foreach (var take in libraryClips)
                    {
                        DrawCard(() =>
                        {
                            GUILayout.Label(take.DisplayName, GuiStyles.CardValue);
                            GUILayout.Label(take.SecondarySummary, GuiStyles.CardLabel);
                            GUILayout.Space(8f);
                            if (GUILayout.Button("Open Playback", GuiStyles.PrimaryButton, GUILayout.Height(36f)))
                            {
                                LoadTake(take);
                            }
                        });
                    }
                    GUILayout.EndScrollView();
                }
            });
        }

        private void DrawStageScreen()
        {
            var safeRect = ReferenceSafeRect;
            var headerRect = new Rect(
                safeRect.x + 16f,
                safeRect.y + 14f,
                safeRect.width - 32f,
                82f
            );
            DrawPanelArea(headerRect, GuiStyles.DarkPanel, () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("‹", GuiStyles.SecondaryButton, GUILayout.Width(44f), GUILayout.Height(42f)))
                {
                    interactor.SetPlaybackActive(false);
                    screen = StudioScreen.Capture;
                }

                GUILayout.Space(10f);
                GUILayout.BeginVertical();
                GUILayout.Label(selectedTake != null ? selectedTake.DisplayName : "Stage Playback", GuiStyles.CardValue);
                GUILayout.Label(selectedClip != null ? $"{selectedClip.Duration:0.00}s • {selectedClip.FrameCount} frames" : "No clip loaded", GuiStyles.Subtitle);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(avatarView != null && avatarView.IsAvailable ? "Avatar" : "Skeleton", GuiStyles.Pill, GUILayout.Width(92f), GUILayout.Height(30f));
                GUILayout.EndHorizontal();
            });

            var bottomRect = new Rect(
                safeRect.x + 20f,
                safeRect.yMax - 182f,
                safeRect.width - 40f,
                162f
            );
            DrawPanelArea(bottomRect, GuiStyles.DarkPanel, () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Model", GuiStyles.SecondaryButton, GUILayout.Height(38f)))
                {
                    ShowTransientMessage(
                        avatarView != null && avatarView.IsAvailable
                            ? "Default humanoid avatar is active."
                            : "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview."
                    );
                }

                if (GUILayout.Button("Record Again", GuiStyles.SecondaryButton, GUILayout.Height(38f)))
                {
                    interactor.SetPlaybackActive(false);
                    screen = StudioScreen.Capture;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(12f);
                GUILayout.BeginHorizontal();
                GUI.enabled = selectedClip != null;
                if (GUILayout.Button(interactor.State.isPlaying ? "Pause" : "Play", GuiStyles.SecondaryButton, GUILayout.Height(44f)))
                {
                    TogglePlayback();
                }

                if (GUILayout.Button("Save", GuiStyles.PrimaryButton, GUILayout.Height(44f)))
                {
                    ShowTransientMessage("Already saved after capture.");
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.Space(10f);
                GUILayout.Label("Drag to orbit and pinch-to-zoom will come with the avatar stage pass.", GuiStyles.CenteredCaption);
            });
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
            var storedTake = archiveStore.LoadStoredTake(take.id);
            selectedClip = storedTake.clip;
            playbackTime = 0f;
            interactor.ReplaceCurrentClip(storedTake.clip, storedTake.sourceClip);
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

        private void DrawShellScreen(Rect shellRect, string title, string subtitle, Action onBack, Action drawContent)
        {
            DrawPanelArea(shellRect, GuiStyles.Window, () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("‹", GuiStyles.SecondaryButton, GUILayout.Width(44f), GUILayout.Height(42f)))
                {
                    onBack();
                }

                GUILayout.Space(10f);
                GUILayout.BeginVertical();
                GUILayout.Label(title, GuiStyles.SectionTitle);
                GUILayout.Label(subtitle, GuiStyles.Subtitle);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(16f);
                drawContent();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Archive root: Application.persistentDataPath/OdoroArchiveV2", GuiStyles.Footnote);
            });
        }

        private void DrawPanelArea(Rect rect, GUIStyle style, Action drawContent)
        {
            GUILayout.BeginArea(rect, GUIContent.none, style);
            drawContent();
            GUILayout.EndArea();
        }

        private void DrawToast()
        {
            if (string.IsNullOrEmpty(transientMessage))
            {
                return;
            }

            var safeRect = ReferenceSafeRect;

            var toastRect = new Rect(
                safeRect.x + 20f,
                safeRect.yMax - 244f,
                safeRect.width - 40f,
                52f
            );
            GUILayout.BeginArea(toastRect, transientMessage, GuiStyles.Toast);
            GUILayout.EndArea();
        }

        private string CaptureProgressTitle()
        {
            if (interactor.State.isRecording)
            {
                return $"Recording {interactor.State.recordingDuration:0.00}s / {recordingContext.FixedCaptureDuration:0.00}s";
            }

            return $"{recordingContext.targetBarCount} bars • {recordingContext.bpm:0} BPM";
        }

        private string CaptureModeLabel()
        {
            return motionSource.CaptureMode switch
            {
                CaptureMode.RearBody3D => "AR Body 3D",
                CaptureMode.FrontUpperBody => "Front Upper",
                CaptureMode.ImportedVideo => "Imported Video",
                _ => "Mock Full Body",
            };
        }

        private Rect GetPreviewRect()
        {
            var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            var width = Mathf.Min(screenRect.width, screenRect.height * PhoneAspect);
            var height = width / PhoneAspect;

            if (height > screenRect.height)
            {
                height = screenRect.height;
                width = height * PhoneAspect;
            }

            return new Rect(
                Mathf.Round((screenRect.width - width) * 0.5f),
                Mathf.Round((screenRect.height - height) * 0.5f),
                Mathf.Round(width),
                Mathf.Round(height)
            );
        }

        private void DrawPhoneFrame(Rect previewRect)
        {
            var shadowRect = new Rect(previewRect.x - 10f, previewRect.y - 10f, previewRect.width + 20f, previewRect.height + 20f);
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            GUI.Box(shadowRect, GUIContent.none, GuiStyles.PhoneFrame);
            GUI.color = new Color(1f, 1f, 1f, 0.08f);
            GUI.Box(previewRect, GUIContent.none, GuiStyles.PhoneFrame);
            GUI.color = Color.white;
        }

        private void UpdateCameraViewport()
        {
            if (mainCamera == null)
            {
                return;
            }

            var previewRect = GetPreviewRect();
            mainCamera.rect = new Rect(
                previewRect.x / Screen.width,
                previewRect.y / Screen.height,
                previewRect.width / Screen.width,
                previewRect.height / Screen.height
            );
        }
    }
}
