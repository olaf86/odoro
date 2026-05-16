using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Odoro
{
    public sealed class StudioL10nEntryDefinition
    {
        public readonly string key;
        public readonly string english;
        public readonly string japanese;

        public StudioL10nEntryDefinition(string key, string english, string japanese)
        {
            this.key = key;
            this.english = english;
            this.japanese = japanese;
        }
    }

    public static class StudioL10n
    {
        public const string TableName = "Studio";

        private static readonly StudioL10nEntryDefinition[] entries =
        {
            new("capture.title", "Capture", "キャプチャ"),
            new("recording-session.title", "Recording Session", "録画セッション"),
            new("recording-session.hint", "Adjust BPM, meter, and count-in before the next take.", "次のテイクの前に BPM、拍子、カウントインを調整します。"),
            new("session.bpm", "BPM", "BPM"),
            new("session.numerator", "Numerator", "拍子の分子"),
            new("session.denominator", "Denominator", "拍子の分母"),
            new("session.count-in-bars", "Count-In Bars", "カウントイン小節"),
            new("session.fixed-duration", "Fixed Capture Duration", "固定録画時間"),
            new("library.title", "Clip Library", "クリップライブラリ"),
            new("library.subtitle", "Review past takes and jump back into playback.", "保存済みテイクを確認して再生に戻れます。"),
            new("library.empty", "No clips yet.", "まだクリップがありません。"),
            new("stage.title-fallback", "Stage Playback", "ステージ再生"),
            new("stage.no-clip", "No clip loaded", "クリップが読み込まれていません"),
            new("stage.avatar-label", "Avatar", "アバター"),
            new("stage.skeleton-label", "Skeleton", "スケルトン"),
            new("capture.hint", "AR capture preview with session controls.", "セッション設定付きの AR キャプチャプレビューです。"),
            new("capture.latest-ready", "Latest clip is ready. Open Stage to preview the take.", "最新のクリップを再生できます。Stage を開いて確認してください。"),
            new("capture.prompt-record", "Tap Record to capture the first take.", "Record を押して最初のテイクを録画してください。"),
            new("stage.hint-avatar-active", "Humanoid avatar preview is active.", "humanoid アバタープレビューが有効です。"),
            new("stage.hint-avatar-missing", "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.", "アバタープレビューを有効にするには Resources/Odoro/DefaultAvatar に humanoid prefab を置いてください。"),
            new("button.open-playback", "Open Playback", "再生を開く"),
            new("button.library", "Library", "ライブラリ"),
            new("button.stage", "Stage", "ステージ"),
            new("button.record", "Record", "録画"),
            new("button.stop", "Stop", "停止"),
            new("button.back", "Back", "戻る"),
            new("button.model", "Model", "モデル"),
            new("button.record-again", "Record Again", "撮り直す"),
            new("button.save", "Save", "保存"),
            new("button.play", "Play", "再生"),
            new("button.pause", "Pause", "一時停止"),
            new("archive.root-caption", "Archive root: Application.persistentDataPath/OdoroArchiveV2", "保存先: Application.persistentDataPath/OdoroArchiveV2"),
            new("status.preview-idle", "Preview is ready.", "プレビュー待機中です。"),
            new("status.recording-motion", "Recording motion...", "モーションを記録しています。"),
            new("status.insufficient-frames", "Not enough frames were captured.", "記録フレームが足りませんでした。"),
            new("status.capture-complete", "Capture complete.", "キャプチャが完了しました。"),
            new("status.mock-preview", "Showing the mock preview.", "mock プレビューを表示しています。"),
            new("status.mock-recording", "Recording with the mock source.", "録画ソースを有効化しました。"),
            new("status.ar-preparing", "Preparing AR body tracking...", "AR body tracking の初期化中です。"),
            new("status.ar-detected", "Body detected. Ready to record.", "身体を検出しました。録画できます。"),
            new("status.ar-lost", "Looking for a full body in frame.", "身体を検出中です。全身がカメラに入るようにしてください。"),
            new("status.ar-unsupported", "AR body tracking is unavailable on this device.", "この環境では AR body tracking を利用できません。"),
            new("toast.take-saved", "Saved a new take.", "新しいテイクを保存しました。"),
            new("toast.save-failed", "Failed to save. Check the Console.", "保存に失敗しました。Console を確認してください。"),
            new("toast.need-capture-first", "Record a take first.", "まずはテイクを録画してください。"),
            new("toast.avatar-active", "Default humanoid avatar is active.", "標準 humanoid アバターが有効です。"),
            new("toast.avatar-missing", "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.", "アバタープレビューを有効にするには Resources/Odoro/DefaultAvatar に humanoid prefab を置いてください。"),
            new("toast.already-saved", "This take is already saved after capture.", "このテイクは録画後にすでに保存されています。"),
            new("toast.recording-started", "Recording started.", "録画を開始しました。"),
            new("toast.stored-take-loaded", "Loaded the saved take.", "保存済みテイクを読み込みました。"),
            new("capture-mode.rear-body-3d", "AR Body 3D", "AR Body 3D"),
            new("capture-mode.front-upper", "Front Upper", "Front Upper"),
            new("capture-mode.imported-video", "Imported Video", "動画インポート"),
            new("capture-mode.mock", "Mock Full Body", "Mock Full Body"),
            new("format.recording-progress", "Recording {0:0.00}s / {1:0.00}s", "録画中 {0:0.00}s / {1:0.00}s"),
            new("format.capture-beat-summary", "{0} bars • {1:0} BPM", "{0} 小節 • {1:0} BPM"),
            new("format.clip-summary", "{0:0.00}s • {1} frames", "{0:0.00}s • {1} フレーム"),
            new("format.recording-session-summary", "{0} BPM • {1}/{2} • {3} bars", "{0} BPM • {1}/{2} • {3} 小節"),
            new("format.fixed-duration-seconds", "{0:0.00} sec", "{0:0.00} 秒"),
        };

        private static readonly Dictionary<string, StudioL10nEntryDefinition> entriesByKey = BuildIndex();

        private static event Action localeChanged;
        private static bool localeSubscriptionRegistered;

        public static IReadOnlyList<StudioL10nEntryDefinition> Entries => entries;

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
        public static string ArchiveRootCaption => Get("archive.root-caption");

        public static string StatusPreviewIdle => Get("status.preview-idle");
        public static string StatusRecordingMotion => Get("status.recording-motion");
        public static string StatusInsufficientFrames => Get("status.insufficient-frames");
        public static string StatusCaptureComplete => Get("status.capture-complete");
        public static string StatusMockPreview => Get("status.mock-preview");
        public static string StatusMockRecording => Get("status.mock-recording");
        public static string StatusArPreparing => Get("status.ar-preparing");
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

        public static string CaptureBeatSummary(int bars, int bpm)
        {
            return Get("format.capture-beat-summary", bars, bpm);
        }

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

        public static string Text(string english, string japanese)
        {
            return UseJapaneseFallback ? japanese : english;
        }

        private static string Get(string key, params object[] arguments)
        {
            if (TryGetLocalizedString(key, arguments, out var localized))
            {
                return localized;
            }

            if (!entriesByKey.TryGetValue(key, out var fallbackEntry))
            {
                return key;
            }

            var template = UseJapaneseFallback ? fallbackEntry.japanese : fallbackEntry.english;
            return FormatFallback(template, arguments);
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

        private static string FormatFallback(string template, object[] arguments)
        {
            if (string.IsNullOrEmpty(template) || arguments == null || arguments.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, arguments);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        private static bool UseJapaneseFallback
        {
            get
            {
                try
                {
                    if (LocalizationSettings.HasSettings)
                    {
                        var selectedLocale = LocalizationSettings.SelectedLocale;
                        var code = selectedLocale?.Identifier.Code;
                        if (!string.IsNullOrEmpty(code))
                        {
                            return code.StartsWith("ja", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                catch (Exception)
                {
                }

                return Application.systemLanguage == SystemLanguage.Japanese;
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

        private static Dictionary<string, StudioL10nEntryDefinition> BuildIndex()
        {
            var index = new Dictionary<string, StudioL10nEntryDefinition>(entries.Length, StringComparer.Ordinal);
            for (var i = 0; i < entries.Length; i += 1)
            {
                index[entries[i].key] = entries[i];
            }

            return index;
        }
    }
}
