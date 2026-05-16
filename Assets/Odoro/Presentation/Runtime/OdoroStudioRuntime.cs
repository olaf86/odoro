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
        private HumanoidAvatarView avatarView;
        private OdoroStudioUiToolkitView uiView;
        private Camera mainCamera;

        private MotionRecordingContext recordingContext;
        private MotionFrame latestPreviewFrame;
        private MotionClip selectedClip;
        private MotionTakeSummary selectedTake;
        private StudioScreen screen = StudioScreen.Capture;
        private List<MotionTakeSummary> libraryClips = new List<MotionTakeSummary>();
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
            ConfigureUi();
            RefreshLibrary();
            motionSource.Activate(MotionSourceActivity.Preview);
            RefreshUi();
        }

        private void OnDestroy()
        {
            motionSource?.Deactivate();
            skeletonView?.Dispose();
            avatarView?.Dispose();
            uiView?.Dispose();
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
                RefreshUi();
            }

            uiView?.ApplySafeArea(Screen.safeArea);
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
            interactor.OnStateChanged += _ => RefreshUi();

            interactor.OnClipChanged += clip =>
            {
                selectedClip = clip;
                playbackTime = 0f;
                if (clip == null)
                {
                    selectedTake = null;
                }

                RefreshUi();
            };

            interactor.OnRecordingCompleted += HandleRecordingCompleted;
        }

        private void ConfigureUi()
        {
            uiView = new OdoroStudioUiToolkitView(gameObject, new OdoroStudioUiActions
            {
                showCapture = ShowCapture,
                showLibrary = ShowLibrary,
                showStage = ShowStage,
                startRecording = StartRecording,
                stopRecording = StopRecording,
                togglePlayback = TogglePlayback,
                showModelInfo = ShowModelInfo,
                saveTake = SaveTakeReminder,
                decreaseBpm = () => AdjustBpm(-5f),
                increaseBpm = () => AdjustBpm(5f),
                decreaseNumerator = () => AdjustNumerator(-1),
                increaseNumerator = () => AdjustNumerator(1),
                decreaseDenominator = () => AdjustDenominator(-1),
                increaseDenominator = () => AdjustDenominator(1),
                decreaseCountInBars = () => AdjustCountInBars(-1),
                increaseCountInBars = () => AdjustCountInBars(1),
                openTake = LoadTake,
            });
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
                RefreshUi();
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
        }

        private void RefreshUi()
        {
            if (uiView == null || interactor == null || motionSource == null)
            {
                return;
            }

            uiView.Render(new OdoroStudioUiSnapshot
            {
                screen = screen,
                state = interactor.State,
                recordingContext = recordingContext,
                captureMode = motionSource.CaptureMode,
                selectedClip = selectedClip,
                selectedTake = selectedTake,
                libraryClips = libraryClips,
                hasAvatar = avatarView != null && avatarView.IsAvailable,
                transientMessage = transientMessage,
                captureHeadline = CaptureProgressTitle(),
                captureSummary = RecordingContextSummary(),
                captureStatus = interactor.State.statusText,
                captureModeLabel = CaptureModeLabel(),
                stageTitle = selectedTake != null ? selectedTake.DisplayName : "Stage Playback",
                stageSummary = selectedClip != null ? $"{selectedClip.Duration:0.00}s • {selectedClip.FrameCount} frames" : "No clip loaded",
                stageModeLabel = avatarView != null && avatarView.IsAvailable ? "Avatar" : "Skeleton",
                stageHint = avatarView != null && avatarView.IsAvailable
                    ? "Humanoid avatar preview is active."
                    : "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.",
            });
        }

        private void ShowCapture()
        {
            interactor.SetPlaybackActive(false);
            screen = StudioScreen.Capture;
            RefreshUi();
        }

        private void ShowLibrary()
        {
            RefreshLibrary();
            screen = StudioScreen.ClipsLibrary;
            RefreshUi();
        }

        private void ShowStage()
        {
            if (selectedClip == null)
            {
                ShowTransientMessage("まずはテイクを録画してください。");
                return;
            }

            screen = StudioScreen.Stage;
            RefreshUi();
        }

        private void ShowModelInfo()
        {
            ShowTransientMessage(
                avatarView != null && avatarView.IsAvailable
                    ? "Default humanoid avatar is active."
                    : "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview."
            );
        }

        private void SaveTakeReminder()
        {
            ShowTransientMessage("Already saved after capture.");
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
            RefreshUi();
        }

        private void StopRecording()
        {
            interactor.StopRecording();
            motionSource.Activate(MotionSourceActivity.Preview);
            RefreshUi();
        }

        private void LoadTake(MotionTakeSummary take)
        {
            selectedTake = take;
            currentSessionId = take.sessionId;
            var storedTake = archiveStore.LoadStoredTake(take.id);
            selectedClip = storedTake.clip;
            playbackTime = 0f;
            interactor.ReplaceCurrentClip(storedTake.clip, storedTake.sourceClip);
            interactor.SetPlaybackActive(false);
            screen = StudioScreen.Stage;
            ShowTransientMessage("保存済みテイクを読み込みました。");
            RefreshUi();
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
            RefreshUi();
        }

        private void AdjustBpm(float delta)
        {
            recordingContext.bpm = Mathf.Clamp(recordingContext.bpm + delta, 60f, 200f);
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
            RefreshUi();
        }

        private void AdjustNumerator(int delta)
        {
            recordingContext.timeSignatureNumerator = Mathf.Clamp(recordingContext.timeSignatureNumerator + delta, 2, 7);
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
            RefreshUi();
        }

        private void AdjustDenominator(int delta)
        {
            var options = new[] { 2, 4, 8 };
            var currentIndex = Mathf.Max(0, Array.IndexOf(options, recordingContext.timeSignatureDenominator));
            var nextIndex = Mathf.Clamp(currentIndex + delta, 0, options.Length - 1);
            recordingContext.timeSignatureDenominator = options[nextIndex];
            recordingContext = recordingContext.NormalizedForFixedCaptureLength();
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
            RefreshUi();
        }

        private void AdjustCountInBars(int delta)
        {
            recordingContext.countInBarCount = Mathf.Clamp(recordingContext.countInBarCount + delta, 0, 4);
            RefreshUi();
        }

        private void ShowTransientMessage(string message)
        {
            transientMessage = message;
            transientMessageExpiresAt = Time.unscaledTime + 2.5f;
            RefreshUi();
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

        private void UpdateCameraViewport()
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
