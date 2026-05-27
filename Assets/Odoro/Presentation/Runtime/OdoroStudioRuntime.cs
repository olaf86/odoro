using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class OdoroStudioRuntime : MonoBehaviour
    {
        private const int CapturePreviewSkeletonFrameInterval = 10;
        private const float TrackingSignalFreshnessSeconds = 0.75f;
        private const float DebugFrameCaptureDuration = 10f;

        private MotionArchiveStore archiveStore;
        private AvatarAssetStore avatarAssetStore;
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
        private List<StageAvatarOption> avatarOptions = new List<StageAvatarOption>();
        private StageAvatarOption selectedAvatarOption;
        private string currentSessionId;
        private string transientMessage;
        private float transientMessageExpiresAt;
        private float playbackTime;
        private bool stageLoopPlayback = true;
        private bool captureSkeletonVisible = true;
        private int capturePreviewSkeletonFrameCounter;
        private bool capturePreviewSkeletonFrameReady;
        private float lastMotionFrameReceivedAt = -1f;
        private float lastMotionFrameSourceTime = -1f;
        private float liveMotionFps;
        private readonly List<MotionFrame> debugCapturedFrames = new List<MotionFrame>();
        private bool debugHudVisible;
        private bool debugFrameCaptureActive;
        private float debugFrameCaptureStartedAt;
        private float debugFrameCaptureFirstSourceTime = -1f;
        private string debugLastSavedPath;
        private string debugReplayPath;
        private string debugLastShareStatus;
        private bool avatarImportInProgress;
        private bool avatarDownloadInProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Object.FindAnyObjectByType<OdoroStudioRuntime>() != null)
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
            avatarAssetStore = new AvatarAssetStore();
            motionSource = CreateMotionSource();
            interactor = new MotionStudioInteractor(
                motionSource,
                recordingContext.FixedCaptureDuration,
                new StudioPlaybackCapturedClipPreparer(motionSource.CaptureMode)
            );
            skeletonView = new SkeletonView("Odoro Skeleton View");
            RefreshAvatarLibrary();
            SelectInitialAvatar();

            ConfigureCamera();
            ConfigureInteractor();
            LoadRecordedReplayClip();
            ConfigureUi();
            RefreshLibrary();
            motionSource.Activate(MotionSourceActivity.Preview);
            StudioL10n.LocaleChanged += RefreshUi;
            RefreshUi();
        }

        private void OnDestroy()
        {
            StudioL10n.LocaleChanged -= RefreshUi;
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
                if (ShouldShowAvatar())
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
                UpdateCaptureSkeletonPreview();
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
#if UNITY_EDITOR
            if (MotionDebugFrameStore.TryReadReplay(out var replayClip, out debugReplayPath))
            {
                return new RecordedMotionSource(replayClip, debugReplayPath);
            }
#endif

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
            motionSource.OnFrame += frame =>
            {
                latestPreviewFrame = frame;
                lastMotionFrameReceivedAt = Time.unscaledTime;
                CaptureDebugFrame(frame);

                if (lastMotionFrameSourceTime >= 0f)
                {
                    var delta = Mathf.Max(0.0001f, frame.time - lastMotionFrameSourceTime);
                    var instantFps = 1f / delta;
                    liveMotionFps = liveMotionFps <= 0f ? instantFps : Mathf.Lerp(liveMotionFps, instantFps, 0.15f);
                }

                lastMotionFrameSourceTime = frame.time;

                if (interactor == null || interactor.State.isRecording)
                {
                    capturePreviewSkeletonFrameReady = true;
                    return;
                }

                capturePreviewSkeletonFrameCounter =
                    (capturePreviewSkeletonFrameCounter + 1) % CapturePreviewSkeletonFrameInterval;
                capturePreviewSkeletonFrameReady = capturePreviewSkeletonFrameCounter == 0;
            };
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

        private void LoadRecordedReplayClip()
        {
            if (motionSource is not RecordedMotionSource recordedSource || !recordedSource.IsSupported)
            {
                return;
            }

            interactor.ReplaceCurrentClip(recordedSource.PlaybackClip, recordedSource.SourceClip);
            latestPreviewFrame = recordedSource.PlaybackClip.Sample(0f);
            capturePreviewSkeletonFrameReady = true;
        }

        private void ConfigureUi()
        {
            uiView = new OdoroStudioUiToolkitView(gameObject, new OdoroStudioUiActions
            {
                showCapture = ShowCapture,
                showSettings = ShowSettings,
                showLibrary = ShowLibrary,
                showStage = ShowStage,
                startRecording = StartRecording,
                stopRecording = StopRecording,
                toggleSkeleton = ToggleCaptureSkeleton,
                togglePlayback = TogglePlayback,
                showModelInfo = ShowModelInfo,
                showModelSelection = ShowModelSelection,
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
                selectAvatarOption = SelectAvatarOption,
                showDebugHud = ShowDebugHud,
                hideDebugHud = HideDebugHud,
                startDebugFrameCapture = StartDebugFrameCapture,
                stopDebugFrameCapture = () => StopDebugFrameCapture(true),
                shareDebugMotionFrames = ShareDebugMotionFrames,
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
                ShowTransientMessage(StudioL10n.ToastTakeSaved);
                RefreshUi();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowTransientMessage(StudioL10n.ToastSaveFailed);
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
                transientMessage = transientMessage,
                capture = new CaptureScreenSnapshot
                {
                    headline = CaptureProgressTitle(),
                    summary = RecordingContextSummary(),
                    status = interactor.State.statusText,
                    modeLabel = CaptureModeLabel(),
                    metrics = CaptureMetricsLabel(),
                    trackingSignal = CaptureTrackingSignalLabel(),
                    trackingSignalColor = CaptureTrackingSignalColor(),
                    progress = CaptureProgressValue(),
                    skeletonVisible = captureSkeletonVisible,
                    hasSelectedClip = selectedClip != null,
                    isRecording = interactor.State.isRecording,
                },
                settings = new RecordingSettingsScreenSnapshot
                {
                    bpm = recordingContext.bpm,
                    timeSignatureNumerator = recordingContext.timeSignatureNumerator,
                    timeSignatureDenominator = recordingContext.timeSignatureDenominator,
                    countInBarCount = recordingContext.countInBarCount,
                    fixedCaptureDuration = recordingContext.FixedCaptureDuration,
                    skeletonVisible = captureSkeletonVisible,
                },
                stage = new StageScreenSnapshot
                {
                    hasSelectedClip = selectedClip != null,
                    isPlaying = interactor.State.isPlaying,
                    title = selectedTake != null ? selectedTake.DisplayName : StudioL10n.StageTitleFallback,
                    summary = selectedClip != null ? StudioL10n.ClipSummary(selectedClip.Duration, selectedClip.FrameCount) : StudioL10n.StageNoClip,
                    modeLabel = StageModeLabel(),
                    hint = StageHintLabel(),
                },
                library = new LibraryScreenSnapshot
                {
                    clips = libraryClips,
                },
                modelSelection = new ModelSelectionScreenSnapshot
                {
                    options = BuildAvatarOptionSnapshots(),
                    selectedOptionId = selectedAvatarOption?.id,
                    isBusy = avatarImportInProgress || avatarDownloadInProgress,
                },
                debug = BuildDebugHudSnapshot(),
            });
        }

        private void ShowCapture()
        {
            interactor.SetPlaybackActive(false);
            screen = StudioScreen.Capture;
            RefreshUi();
        }

        private void ShowSettings()
        {
            if (interactor.State.isRecording)
            {
                return;
            }

            interactor.SetPlaybackActive(false);
            screen = StudioScreen.RecordingSettings;
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
                ShowTransientMessage(StudioL10n.ToastNeedCaptureFirst);
                return;
            }

            screen = StudioScreen.Stage;
            RefreshUi();
        }

        private void ShowModelInfo()
        {
            ShowTransientMessage(
                ShouldShowAvatar()
                    ? StudioL10n.ToastAvatarLoaded(selectedAvatarOption?.title ?? avatarView.DisplayName)
                    : selectedAvatarOption?.title ?? StudioL10n.ToastAvatarMissing
            );
        }

        private void ShowModelSelection()
        {
            RefreshAvatarLibrary();
            screen = StudioScreen.ModelSelection;
            RefreshUi();
        }

        private void RefreshAvatarLibrary()
        {
            avatarOptions = new List<StageAvatarOption>(avatarAssetStore.FetchAvailableOptions());
            if (selectedAvatarOption == null)
            {
                return;
            }

            var matchingOption = FindAvatarOption(selectedAvatarOption.id);
            if (matchingOption != null)
            {
                selectedAvatarOption = matchingOption;
            }
        }

        private void SelectInitialAvatar()
        {
            var storedSelection = PlayerPrefs.GetString("Odoro.SelectedAvatarOption", string.Empty);
            var option = FindAvatarOption(storedSelection) ?? FirstAvatarOption() ?? FirstSkeletonOption();
            ApplyAvatarOption(option, false);
        }

        private StageAvatarOption FirstAvatarOption()
        {
            for (var optionIndex = 0; optionIndex < avatarOptions.Count; optionIndex += 1)
            {
                if (avatarOptions[optionIndex].UsesAvatar)
                {
                    return avatarOptions[optionIndex];
                }
            }

            return null;
        }

        private StageAvatarOption FirstSkeletonOption()
        {
            return avatarOptions.Count > 0 ? avatarOptions[0] : null;
        }

        private StageAvatarOption FindAvatarOption(string optionId)
        {
            if (string.IsNullOrEmpty(optionId))
            {
                return null;
            }

            for (var optionIndex = 0; optionIndex < avatarOptions.Count; optionIndex += 1)
            {
                if (avatarOptions[optionIndex].id == optionId)
                {
                    return avatarOptions[optionIndex];
                }
            }

            return null;
        }

        private void SelectAvatarOption(string optionId)
        {
            var option = FindAvatarOption(optionId);
            if (option == null || avatarImportInProgress || avatarDownloadInProgress)
            {
                return;
            }

            if (option.RequiresDownload)
            {
                DownloadAvatarOption(option);
                return;
            }

            ApplyAvatarOption(option, true);
        }

        private async void DownloadAvatarOption(StageAvatarOption option)
        {
            try
            {
                avatarDownloadInProgress = true;
                ShowTransientMessage(StudioL10n.ToastAvatarDownloading(option.title));
                var installedOption = await avatarAssetStore.InstallDownloadableAvatarAsync(option);
                RefreshAvatarLibrary();
                ApplyAvatarOption(FindAvatarOption(installedOption.id) ?? installedOption, true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowTransientMessage(StudioL10n.ToastAvatarImportFailed(exception.Message));
            }
            finally
            {
                avatarDownloadInProgress = false;
                RefreshUi();
            }
        }

        private async void ApplyAvatarOption(StageAvatarOption option, bool showResult)
        {
            if (option == null)
            {
                return;
            }

            selectedAvatarOption = option;
            PlayerPrefs.SetString("Odoro.SelectedAvatarOption", option.id);
            PlayerPrefs.Save();

            avatarView?.Dispose();
            avatarView = null;

            if (option.kind == StageAvatarOptionKind.ProceduralSkeleton)
            {
                if (showResult)
                {
                    ShowTransientMessage(option.title);
                }

                RefreshUi();
                return;
            }

            try
            {
                avatarImportInProgress = true;
                ShowTransientMessage(StudioL10n.ToastAvatarLoading);

                avatarView = option.kind switch
                {
                    StageAvatarOptionKind.ResourcesPrefab => HumanoidAvatarView.TryCreateFromResources(option.resourcePath),
                    StageAvatarOptionKind.LocalDevelopmentGlb => await GltfAvatarLoader.LoadAsync(option.runtimeAssetPath, option.title),
                    StageAvatarOptionKind.DownloadableGlb => await GltfAvatarLoader.LoadAsync(option.runtimeAssetPath, option.title),
                    _ => null,
                };

                if (avatarView == null)
                {
                    throw new InvalidOperationException("No supported avatar rig was found.");
                }

                if (showResult)
                {
                    ShowTransientMessage(StudioL10n.ToastAvatarLoaded(option.title));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                selectedAvatarOption = FirstSkeletonOption();
                PlayerPrefs.SetString("Odoro.SelectedAvatarOption", selectedAvatarOption?.id ?? string.Empty);
                ShowTransientMessage(StudioL10n.ToastAvatarImportFailed(exception.Message));
            }
            finally
            {
                avatarImportInProgress = false;
                RefreshUi();
            }
        }

        private void SaveTakeReminder()
        {
            ShowTransientMessage(StudioL10n.ToastAlreadySaved);
        }

        private string RecordingContextSummary()
        {
            return StudioL10n.RecordingSessionSummary(
                Mathf.RoundToInt(recordingContext.bpm),
                recordingContext.timeSignatureNumerator,
                recordingContext.timeSignatureDenominator,
                recordingContext.targetBarCount
            );
        }

        private void StartRecording()
        {
            capturePreviewSkeletonFrameCounter = 0;
            capturePreviewSkeletonFrameReady = true;
            motionSource.Activate(MotionSourceActivity.Recording);
            interactor.UpdateMaximumCaptureDuration(recordingContext.FixedCaptureDuration);
            interactor.BeginRecording();
            ShowTransientMessage(StudioL10n.ToastRecordingStarted);
            RefreshUi();
        }

        private void StopRecording()
        {
            interactor.StopRecording();
            capturePreviewSkeletonFrameCounter = 0;
            capturePreviewSkeletonFrameReady = true;
            motionSource.Activate(MotionSourceActivity.Preview);
            RefreshUi();
        }

        private void ToggleCaptureSkeleton()
        {
            captureSkeletonVisible = !captureSkeletonVisible;
            if (!captureSkeletonVisible)
            {
                skeletonView.SetFrame(null, Palette.CaptureSkeleton);
            }

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
            ShowTransientMessage(StudioL10n.ToastStoredTakeLoaded);
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
                return StudioL10n.RecordingBeatProgress(CurrentRecordingBeat(), TotalRecordingBeats());
            }

            return StudioL10n.CaptureBeatSummary(recordingContext.targetBarCount, Mathf.RoundToInt(recordingContext.bpm));
        }

        private string CaptureModeLabel()
        {
            return StudioL10n.CaptureModeTitle(motionSource.CaptureMode);
        }

        private string StageModeLabel()
        {
            if (avatarImportInProgress)
            {
                return StudioL10n.ToastAvatarLoading;
            }

            if (avatarDownloadInProgress)
            {
                return StudioL10n.ToastAvatarDownloading(selectedAvatarOption?.title ?? StudioL10n.AvatarLabel);
            }

            if (ShouldShowAvatar())
            {
                return selectedAvatarOption?.title ?? StudioL10n.AvatarLabel;
            }

            return StudioL10n.SkeletonLabel;
        }

        private string StageHintLabel()
        {
            if (selectedAvatarOption == null || selectedAvatarOption.kind == StageAvatarOptionKind.ProceduralSkeleton)
            {
                return StudioL10n.ModelSkeletonPreview;
            }

            return ShouldShowAvatar()
                ? StudioL10n.StageHintAvatarActive
                : StudioL10n.StageHintAvatarMissing;
        }

        private bool ShouldShowAvatar()
        {
            return selectedAvatarOption != null
                && selectedAvatarOption.UsesAvatar
                && avatarView != null
                && avatarView.IsAvailable;
        }

        private IReadOnlyList<StageAvatarOptionSnapshot> BuildAvatarOptionSnapshots()
        {
            var snapshots = new List<StageAvatarOptionSnapshot>();
            for (var optionIndex = 0; optionIndex < avatarOptions.Count; optionIndex += 1)
            {
                var option = avatarOptions[optionIndex];
                snapshots.Add(new StageAvatarOptionSnapshot
                {
                    id = option.id,
                    title = option.title,
                    subtitle = option.subtitle,
                    isSelected = selectedAvatarOption != null && selectedAvatarOption.id == option.id,
                    usesAvatar = option.UsesAvatar,
                    requiresDownload = option.RequiresDownload,
                });
            }

            return snapshots;
        }

        private string CaptureMetricsLabel()
        {
            return StudioL10n.CaptureMetrics(liveMotionFps, Screen.width, Screen.height);
        }

        private string CaptureTrackingSignalLabel()
        {
            var age = lastMotionFrameReceivedAt < 0f ? float.PositiveInfinity : Time.unscaledTime - lastMotionFrameReceivedAt;
            if (age <= TrackingSignalFreshnessSeconds)
            {
                return StudioL10n.TrackingGood;
            }

            return StudioL10n.TrackingSearching;
        }

        private Color CaptureTrackingSignalColor()
        {
            var age = lastMotionFrameReceivedAt < 0f ? float.PositiveInfinity : Time.unscaledTime - lastMotionFrameReceivedAt;
            return age <= TrackingSignalFreshnessSeconds
                ? new Color(0.18f, 0.82f, 0.43f)
                : new Color(0.96f, 0.72f, 0.22f);
        }

        private float CaptureProgressValue()
        {
            var duration = recordingContext.FixedCaptureDuration;
            if (duration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(interactor.State.recordingDuration / duration);
        }

        private int TotalRecordingBeats()
        {
            return Mathf.Max(1, recordingContext.targetBarCount * recordingContext.timeSignatureNumerator);
        }

        private int CurrentRecordingBeat()
        {
            if (!interactor.State.isRecording)
            {
                return 0;
            }

            var rawBeat = Mathf.FloorToInt(interactor.State.recordingDuration * recordingContext.bpm / 60f);
            return Mathf.Clamp(rawBeat, 0, TotalRecordingBeats());
        }

        private void UpdateCaptureSkeletonPreview()
        {
            if (!captureSkeletonVisible)
            {
                skeletonView.SetFrame(null, Palette.CaptureSkeleton);
                return;
            }

            if (interactor.State.isRecording)
            {
                skeletonView.SetFrame(latestPreviewFrame, Palette.CaptureSkeleton);
                return;
            }

            if (capturePreviewSkeletonFrameReady)
            {
                skeletonView.SetFrame(latestPreviewFrame, Palette.CaptureSkeleton);
                capturePreviewSkeletonFrameReady = false;
            }
        }

        private void UpdateCameraViewport()
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        private bool DebugHudAvailable()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private DebugHudSnapshot BuildDebugHudSnapshot()
        {
            if (!DebugHudAvailable())
            {
                return new DebugHudSnapshot { isAvailable = false };
            }

            var lines = new List<string>
            {
                $"Source: {motionSource?.GetType().Name ?? "--"} ({motionSource?.CaptureMode.ToString() ?? "--"})",
                $"Status: {interactor?.State.statusText ?? "--"}",
                $"Motion FPS: {liveMotionFps:0.0}",
                $"Frame age: {DebugFrameAgeLabel()}",
                $"Joint count: {latestPreviewFrame?.jointPositions?.Length ?? 0}",
                DebugJointLabel(OdoroJointName.Root),
                DebugJointLabel(OdoroJointName.Head),
                DebugJointLabel(OdoroJointName.LeftWrist),
                DebugJointLabel(OdoroJointName.RightWrist),
                $"Replay file: {(HasProjectReplayFile() ? "found" : "missing")}",
            };

            if (!string.IsNullOrEmpty(debugReplayPath))
            {
                lines.Add($"Replay: {debugReplayPath}");
            }

            if (!string.IsNullOrEmpty(debugLastSavedPath))
            {
                lines.Add($"Saved: {debugLastSavedPath}");
            }

            if (!string.IsNullOrEmpty(debugLastShareStatus))
            {
                lines.Add(debugLastShareStatus);
            }

            if (debugFrameCaptureActive)
            {
                lines.Add($"Capturing: {debugCapturedFrames.Count} frames / {DebugFrameCaptureRemainingSeconds():0.0}s");
            }

            return new DebugHudSnapshot
            {
                isAvailable = true,
                isVisible = debugHudVisible,
                isCapturing = debugFrameCaptureActive,
                canShare = DebugFileSharer.IsAvailable && HasShareableDebugMotionFile(),
                lines = lines.ToArray(),
                captureButtonLabel = debugFrameCaptureActive ? "Stop & Save MotionFrames" : "Save Next 10s MotionFrames",
            };
        }

        private void ShowDebugHud()
        {
            debugHudVisible = true;
            RefreshUi();
        }

        private void HideDebugHud()
        {
            debugHudVisible = false;
            RefreshUi();
        }

        private void StartDebugFrameCapture()
        {
            debugCapturedFrames.Clear();
            debugFrameCaptureActive = true;
            debugFrameCaptureStartedAt = Time.unscaledTime;
            debugFrameCaptureFirstSourceTime = -1f;
            debugLastSavedPath = null;
            debugLastShareStatus = null;
            RefreshUi();
        }

        private void CaptureDebugFrame(MotionFrame frame)
        {
            if (!debugFrameCaptureActive || frame == null || frame.jointPositions == null)
            {
                return;
            }

            if (debugFrameCaptureFirstSourceTime < 0f)
            {
                debugFrameCaptureFirstSourceTime = frame.time;
            }

            var relativeTime = Mathf.Max(0f, frame.time - debugFrameCaptureFirstSourceTime);
            debugCapturedFrames.Add(frame.CloneWithTime(relativeTime));

            if (Time.unscaledTime - debugFrameCaptureStartedAt >= DebugFrameCaptureDuration)
            {
                StopDebugFrameCapture(true);
                return;
            }

            if (debugHudVisible)
            {
                RefreshUi();
            }
        }

        private void StopDebugFrameCapture(bool save)
        {
            debugFrameCaptureActive = false;
            if (!save || debugCapturedFrames.Count < 2)
            {
                return;
            }

            var clip = new MotionClip();
            clip.frames.AddRange(debugCapturedFrames);

            try
            {
                debugLastSavedPath = MotionDebugFrameStore.WriteReplay(
                    clip,
                    motionSource.CaptureMode,
                    Application.platform.ToString(),
                    motionSource.GetType().Name
                );
                debugReplayPath = MotionDebugFrameStore.DefaultReplayPath;
                debugLastShareStatus = null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                debugLastSavedPath = $"Save failed: {exception.Message}";
            }

            RefreshUi();
        }

        private bool HasShareableDebugMotionFile()
        {
            return System.IO.File.Exists(MotionDebugFrameStore.DefaultReplayPath);
        }

        private bool HasProjectReplayFile()
        {
#if UNITY_EDITOR
            return System.IO.File.Exists(MotionDebugFrameStore.ProjectReplayPath);
#else
            return false;
#endif
        }

        private void ShareDebugMotionFrames()
        {
            var path = MotionDebugFrameStore.DefaultReplayPath;
            debugLastShareStatus = DebugFileSharer.ShareFile(path)
                ? "Sharing MotionFrames..."
                : "Share failed: no debug MotionFrames file.";
            RefreshUi();
        }

        private float DebugFrameCaptureRemainingSeconds()
        {
            return Mathf.Max(0f, DebugFrameCaptureDuration - (Time.unscaledTime - debugFrameCaptureStartedAt));
        }

        private string DebugFrameAgeLabel()
        {
            if (lastMotionFrameReceivedAt < 0f)
            {
                return "--";
            }

            return $"{Time.unscaledTime - lastMotionFrameReceivedAt:0.00}s";
        }

        private string DebugJointLabel(OdoroJointName jointName)
        {
            var positions = latestPreviewFrame?.jointPositions;
            var index = OdoroSkeletonDefinition.IndexOf(jointName);
            if (positions == null || index < 0 || index >= positions.Length)
            {
                return $"{jointName}: --";
            }

            var position = positions[index];
            return $"{jointName}: ({position.x:0.00}, {position.y:0.00}, {position.z:0.00})";
        }
    }
}
