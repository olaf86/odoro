namespace Odoro.Editor
{
    internal sealed class StudioLocalizationSeedEntry
    {
        public readonly string key;
        public readonly string english;
        public readonly string japanese;

        public StudioLocalizationSeedEntry(string key, string english, string japanese)
        {
            this.key = key;
            this.english = english;
            this.japanese = japanese;
        }
    }

    internal static class StudioLocalizationSeed
    {
        public static readonly StudioLocalizationSeedEntry[] Entries =
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
            new("model-selection.title", "Model Selection", "モデル選択"),
            new("model-selection.subtitle", "Choose skeleton preview or a bundled humanoid prefab.", "スケルトンプレビュー、または同梱 humanoid prefab を選択します。"),
            new("model-selection.selected", "Selected", "選択中"),
            new("model-selection.tap-to-use", "Tap to use this model", "タップしてこのモデルを使う"),
            new("model-selection.not-installed", "Not installed", "未インストール"),
            new("model-selection.skeleton-preview", "Skeleton preview", "スケルトンプレビュー"),
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
            new("button.use", "Use", "使う"),
            new("button.install", "Install", "インストール"),
            new("button.uninstall", "Uninstall", "アンインストール"),
            new("archive.root-caption", "Archive root: Application.persistentDataPath/OdoroArchiveV2", "保存先: Application.persistentDataPath/OdoroArchiveV2"),
            new("status.preview-idle", "Preview is ready.", "プレビュー待機中です。"),
            new("status.recording-motion", "Recording motion...", "モーションを記録しています。"),
            new("status.insufficient-frames", "Not enough frames were captured.", "記録フレームが足りませんでした。"),
            new("status.capture-complete", "Capture complete.", "キャプチャが完了しました。"),
            new("status.mock-preview", "Showing the mock preview.", "mock プレビューを表示しています。"),
            new("status.mock-recording", "Recording with the mock source.", "録画ソースを有効化しました。"),
            new("status.ar-preparing", "Preparing AR body tracking...", "AR body tracking の初期化中です。"),
            new("status.ar-checking-availability", "Checking AR availability...", "AR 利用可否を確認しています。"),
            new("status.ar-needs-install", "Additional AR support is required on this device.", "この端末では追加の AR サポートが必要です。"),
            new("status.ar-needs-camera-permission", "Waiting for camera permission...", "カメラ権限を待っています。"),
            new("status.ar-session-initializing", "AR session is starting. Keep the device steady.", "AR セッションを開始しています。端末を安定させてください。"),
            new("status.ar-detected", "Body detected. Ready to record.", "身体を検出しました。録画できます。"),
            new("status.ar-lost", "Looking for a full body in frame.", "身体を検出中です。全身がカメラに入るようにしてください。"),
            new("status.ar-unsupported", "AR body tracking is unavailable on this device.", "この環境では AR body tracking を利用できません。"),
            new("toast.take-saved", "Saved a new take.", "新しいテイクを保存しました。"),
            new("toast.save-failed", "Failed to save. Check the Console.", "保存に失敗しました。Console を確認してください。"),
            new("toast.need-capture-first", "Record a take first.", "まずはテイクを録画してください。"),
            new("toast.avatar-missing", "Place a humanoid prefab at Resources/Odoro/DefaultAvatar to enable avatar preview.", "アバタープレビューを有効にするには Resources/Odoro/DefaultAvatar に humanoid prefab を置いてください。"),
            new("toast.avatar-loaded", "Loaded avatar model: {0}", "アバターモデルを読み込みました: {0}"),
            new("toast.avatar-installed", "Installed avatar model: {0}", "アバターモデルをインストールしました: {0}"),
            new("toast.avatar-uninstalled", "Uninstalled avatar model: {0}", "アバターモデルをアンインストールしました: {0}"),
            new("toast.avatar-import-failed", "Avatar import failed: {0}", "アバター取り込みに失敗しました: {0}"),
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
    }
}
