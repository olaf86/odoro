using System;
using UnityEngine;

namespace Odoro
{
    public static class StudioL10n
    {
        private static bool UseJapanese => Application.systemLanguage == SystemLanguage.Japanese;

        public static string CaptureTitle => Text("Capture", "キャプチャ");
        public static string RecordingSessionTitle => Text("Recording Session", "録画セッション");
        public static string RecordingSessionHint => Text("Adjust BPM, meter, and count-in before the next take.", "次のテイクの前に BPM、拍子、カウントインを調整します。");
        public static string SessionBpmTitle => Text("BPM", "BPM");
        public static string SessionNumeratorTitle => Text("Numerator", "拍子の分子");
        public static string SessionDenominatorTitle => Text("Denominator", "拍子の分母");
        public static string SessionCountInTitle => Text("Count-In Bars", "カウントイン小節");
        public static string FixedCaptureDurationTitle => Text("Fixed Capture Duration", "固定録画時間");
        public static string LibraryTitle => Text("Clip Library", "クリップライブラリ");
        public static string LibrarySubtitle => Text("Review past takes and jump back into playback.", "保存済みテイクを確認して再生に戻れます。");
        public static string LibraryEmpty => Text("No clips yet.", "まだクリップがありません。");
        public static string StageTitleFallback => Text("Stage Playback", "ステージ再生");
        public static string StageNoClip => Text("No clip loaded", "クリップが読み込まれていません");
        public static string AvatarLabel => Text("Avatar", "アバター");
        public static string SkeletonLabel => Text("Skeleton", "スケルトン");
        public static string CaptureHint => Text("AR capture preview with session controls.", "セッション設定付きの AR キャプチャプレビューです。");
        public static string CaptureLatestClipReady => Text("Latest clip is ready. Open Stage to preview the take.", "最新のクリップを再生できます。Stage を開いて確認してください。");
        public static string CapturePromptToRecord => Text("Tap Record to capture the first take.", "Record を押して最初のテイクを録画してください。");
        public static string StageHintAvatarActive => Text("Humanoid avatar preview is active.", "humanoid アバタープレビューが有効です。");
        public static string StageHintAvatarMissing => Text("Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.", "アバタープレビューを有効にするには Resources/Odoro/DefaultAvatar に humanoid prefab を置いてください。");
        public static string OpenPlayback => Text("Open Playback", "再生を開く");
        public static string ButtonLibrary => Text("Library", "ライブラリ");
        public static string ButtonStage => Text("Stage", "ステージ");
        public static string ButtonRecord => Text("Record", "録画");
        public static string ButtonStop => Text("Stop", "停止");
        public static string ButtonBack => Text("Back", "戻る");
        public static string ButtonModel => Text("Model", "モデル");
        public static string ButtonRecordAgain => Text("Record Again", "撮り直す");
        public static string ButtonSave => Text("Save", "保存");
        public static string ButtonPlay => Text("Play", "再生");
        public static string ButtonPause => Text("Pause", "一時停止");
        public static string ArchiveRootCaption => Text("Archive root: Application.persistentDataPath/OdoroArchiveV2", "保存先: Application.persistentDataPath/OdoroArchiveV2");

        public static string StatusPreviewIdle => Text("Preview is ready.", "プレビュー待機中です。");
        public static string StatusRecordingMotion => Text("Recording motion...", "モーションを記録しています。");
        public static string StatusInsufficientFrames => Text("Not enough frames were captured.", "記録フレームが足りませんでした。");
        public static string StatusCaptureComplete => Text("Capture complete.", "キャプチャが完了しました。");
        public static string StatusMockPreview => Text("Showing the mock preview.", "mock プレビューを表示しています。");
        public static string StatusMockRecording => Text("Recording with the mock source.", "録画ソースを有効化しました。");
        public static string StatusArPreparing => Text("Preparing AR body tracking...", "AR body tracking の初期化中です。");
        public static string StatusArDetected => Text("Body detected. Ready to record.", "身体を検出しました。録画できます。");
        public static string StatusArLost => Text("Looking for a full body in frame.", "身体を検出中です。全身がカメラに入るようにしてください。");
        public static string StatusArUnsupported => Text("AR body tracking is unavailable on this device.", "この環境では AR body tracking を利用できません。");

        public static string ToastTakeSaved => Text("Saved a new take.", "新しいテイクを保存しました。");
        public static string ToastSaveFailed => Text("Failed to save. Check the Console.", "保存に失敗しました。Console を確認してください。");
        public static string ToastNeedCaptureFirst => Text("Record a take first.", "まずはテイクを録画してください。");
        public static string ToastAvatarActive => Text("Default humanoid avatar is active.", "Default humanoid avatar is active.");
        public static string ToastAvatarMissing => Text("Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.", "アバタープレビューを有効にするには Resources/Odoro/DefaultAvatar に humanoid prefab を置いてください。");
        public static string ToastAlreadySaved => Text("This take is already saved after capture.", "このテイクは録画後にすでに保存されています。");
        public static string ToastRecordingStarted => Text("Recording started.", "録画を開始しました。");
        public static string ToastStoredTakeLoaded => Text("Loaded the saved take.", "保存済みテイクを読み込みました。");

        public static string CaptureModeTitle(CaptureMode captureMode)
        {
            return captureMode switch
            {
                CaptureMode.RearBody3D => Text("AR Body 3D", "AR Body 3D"),
                CaptureMode.FrontUpperBody => Text("Front Upper", "Front Upper"),
                CaptureMode.ImportedVideo => Text("Imported Video", "動画インポート"),
                _ => Text("Mock Full Body", "Mock Full Body"),
            };
        }

        public static string RecordingProgress(float current, float duration)
        {
            return UseJapanese
                ? $"録画中 {current:0.00}s / {duration:0.00}s"
                : $"Recording {current:0.00}s / {duration:0.00}s";
        }

        public static string CaptureBeatSummary(int bars, int bpm)
        {
            return UseJapanese
                ? $"{bars} bars • {bpm:0} BPM"
                : $"{bars} bars • {bpm:0} BPM";
        }

        public static string ClipSummary(float durationSeconds, int frameCount)
        {
            return UseJapanese
                ? $"{durationSeconds:0.00}s • {frameCount} フレーム"
                : $"{durationSeconds:0.00}s • {frameCount} frames";
        }

        public static string RecordingSessionSummary(int bpm, int numerator, int denominator, int bars)
        {
            return $"{bpm} BPM • {numerator}/{denominator} • {bars} bars";
        }

        public static string Text(string english, string japanese)
        {
            return UseJapanese ? japanese : english;
        }
    }
}
