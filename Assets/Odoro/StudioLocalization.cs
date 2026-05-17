using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Odoro
{
    public static class StudioL10n
    {
        public const string TableName = "Studio";

        private static event Action localeChanged;
        private static bool localeSubscriptionRegistered;

        public static event Action LocaleChanged
        {
            add
            {
                EnsureLocaleChangeSubscription();
                localeChanged += value;
            }
            remove => localeChanged -= value;
        }

        public static string CaptureTitle => Get("capture.title");
        public static string RecordingSessionTitle => Get("recording-session.title");
        public static string RecordingSessionHint => Get("recording-session.hint");
        public static string SessionBpmTitle => Get("session.bpm");
        public static string SessionNumeratorTitle => Get("session.numerator");
        public static string SessionDenominatorTitle => Get("session.denominator");
        public static string SessionCountInTitle => Get("session.count-in-bars");
        public static string FixedCaptureDurationTitle => Get("session.fixed-duration");
        public static string LibraryTitle => Get("library.title");
        public static string LibrarySubtitle => Get("library.subtitle");
        public static string LibraryEmpty => Get("library.empty");
        public static string StageTitleFallback => Get("stage.title-fallback");
        public static string StageNoClip => Get("stage.no-clip");
        public static string AvatarLabel => Get("stage.avatar-label");
        public static string SkeletonLabel => Get("stage.skeleton-label");
        public static string CaptureHint => Get("capture.hint");
        public static string CaptureLatestClipReady => Get("capture.latest-ready");
        public static string CapturePromptToRecord => Get("capture.prompt-record");
        public static string StageHintAvatarActive => Get("stage.hint-avatar-active");
        public static string StageHintAvatarMissing => Get("stage.hint-avatar-missing");
        public static string OpenPlayback => Get("button.open-playback");
        public static string ButtonLibrary => Get("button.library");
        public static string ButtonStage => Get("button.stage");
        public static string ButtonRecord => Get("button.record");
        public static string ButtonStop => Get("button.stop");
        public static string ButtonBack => Get("button.back");
        public static string ButtonModel => Get("button.model");
        public static string ButtonRecordAgain => Get("button.record-again");
        public static string ButtonSave => Get("button.save");
        public static string ButtonPlay => Get("button.play");
        public static string ButtonPause => Get("button.pause");
        public static string ButtonSettings => Get("button.settings");
        public static string ButtonDone => Get("button.done");
        public static string ButtonSkeletonOn => Get("button.skeleton-on");
        public static string ButtonSkeletonOff => Get("button.skeleton-off");
        public static string ArchiveRootCaption => Get("archive.root-caption");

        public static string StatusPreviewIdle => Get("status.preview-idle");
        public static string StatusRecordingMotion => Get("status.recording-motion");
        public static string StatusInsufficientFrames => Get("status.insufficient-frames");
        public static string StatusCaptureComplete => Get("status.capture-complete");
        public static string StatusMockPreview => Get("status.mock-preview");
        public static string StatusMockRecording => Get("status.mock-recording");
        public static string StatusArPreparing => Get("status.ar-preparing");
        public static string StatusArCheckingAvailability => Get("status.ar-checking-availability");
        public static string StatusArNeedsInstall => Get("status.ar-needs-install");
        public static string StatusArNeedsCameraPermission => Get("status.ar-needs-camera-permission");
        public static string StatusArSessionInitializing => Get("status.ar-session-initializing");
        public static string StatusArDetected => Get("status.ar-detected");
        public static string StatusArLost => Get("status.ar-lost");
        public static string StatusArUnsupported => Get("status.ar-unsupported");

        public static string ToastTakeSaved => Get("toast.take-saved");
        public static string ToastSaveFailed => Get("toast.save-failed");
        public static string ToastNeedCaptureFirst => Get("toast.need-capture-first");
        public static string ToastAvatarActive => Get("toast.avatar-active");
        public static string ToastAvatarMissing => Get("toast.avatar-missing");
        public static string ToastAlreadySaved => Get("toast.already-saved");
        public static string ToastRecordingStarted => Get("toast.recording-started");
        public static string ToastStoredTakeLoaded => Get("toast.stored-take-loaded");

        public static string CaptureModeTitle(CaptureMode captureMode)
        {
            return captureMode switch
            {
                CaptureMode.RearBody3D => Get("capture-mode.rear-body-3d"),
                CaptureMode.FrontUpperBody => Get("capture-mode.front-upper"),
                CaptureMode.ImportedVideo => Get("capture-mode.imported-video"),
                _ => Get("capture-mode.mock"),
            };
        }

        public static string RecordingProgress(float current, float duration)
        {
            return Get("format.recording-progress", current, duration);
        }

        public static string RecordingBeatProgress(int currentBeat, int totalBeats)
        {
            return Get("format.recording-beat-progress", currentBeat, totalBeats);
        }

        public static string CaptureBeatSummary(int bars, int bpm)
        {
            return Get("format.capture-beat-summary", bars, bpm);
        }

        public static string CaptureMetrics(float fps, int width, int height)
        {
            return Get("format.capture-metrics", fps, width, height);
        }

        public static string SkeletonState(bool isVisible)
        {
            return isVisible ? Get("format.skeleton-on") : Get("format.skeleton-off");
        }

        public static string TrackingGood => Get("tracking.good");
        public static string TrackingSearching => Get("tracking.searching");

        public static string ClipSummary(float durationSeconds, int frameCount)
        {
            return Get("format.clip-summary", durationSeconds, frameCount);
        }

        public static string RecordingSessionSummary(int bpm, int numerator, int denominator, int bars)
        {
            return Get("format.recording-session-summary", bpm, numerator, denominator, bars);
        }

        public static string FixedDurationSeconds(float seconds)
        {
            return Get("format.fixed-duration-seconds", seconds);
        }

        private static string Get(string key, params object[] arguments)
        {
            if (TryGetLocalizedString(key, arguments, out var localized))
            {
                return localized;
            }

            return key;
        }

        private static bool TryGetLocalizedString(string key, object[] arguments, out string localized)
        {
            localized = null;

            try
            {
                if (!LocalizationSettings.HasSettings)
                {
                    return false;
                }

                LocalizationSettings.InitializationOperation.WaitForCompletion();
                localized = LocalizationSettings.StringDatabase.GetLocalizedString(
                    TableName,
                    key,
                    null,
                    FallbackBehavior.UseProjectSettings,
                    arguments ?? Array.Empty<object>()
                );

                return !string.IsNullOrEmpty(localized) &&
                       !localized.StartsWith("No translation found", StringComparison.Ordinal);
            }
            catch (Exception)
            {
                localized = null;
                return false;
            }
        }

        private static void EnsureLocaleChangeSubscription()
        {
            if (localeSubscriptionRegistered)
            {
                return;
            }

            LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
            localeSubscriptionRegistered = true;
        }

        private static void HandleSelectedLocaleChanged(Locale _)
        {
            localeChanged?.Invoke();
        }
    }
}
