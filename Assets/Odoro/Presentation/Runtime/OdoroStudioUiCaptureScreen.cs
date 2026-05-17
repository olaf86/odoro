using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class CaptureScreenView
    {
        public VisualElement Root { get; }

        private readonly OdoroStudioUiActions actions;
        private Label headlineLabel;
        private Label summaryLabel;
        private Label modeLabel;
        private Label statusLabel;
        private Label metricsLabel;
        private Label skeletonStateLabel;
        private Label trackingSignalLabel;
        private VisualElement trackingDot;
        private VisualElement progressFill;
        private Label footerLabel;
        private ButtonBinding libraryButton;
        private ButtonBinding stageButton;
        private ButtonBinding settingsButton;
        private ButtonBinding recordButton;
        private ButtonBinding stopButton;

        public CaptureScreenView(VisualElement parent, OdoroStudioUiActions actions)
        {
            this.actions = actions;
            Root = CreateScreen("capture-screen");
            parent.Add(Root);

            var topRow = CreateRow();
            topRow.style.alignItems = Align.FlexStart;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.marginTop = 2f;
            Root.Add(topRow);

            var header = CreatePanel(new Color(0.14f, 0.17f, 0.22f, 0.84f), 22f);
            header.style.width = 224f;
            headlineLabel = CreateTitleLabel();
            summaryLabel = CreateSubtitleLabel();
            modeLabel = CreatePillLabel();
            statusLabel = CreateBodyLabel();
            metricsLabel = CreateCaptionLabel(string.Empty);
            skeletonStateLabel = CreateCaptionLabel(string.Empty);
            trackingSignalLabel = CreateValueLabel();
            trackingDot = CreateTrackingDot();

            var trackingRow = CreateRow();
            trackingRow.style.alignItems = Align.Center;
            trackingRow.Add(trackingDot);
            trackingRow.Add(trackingSignalLabel);

            header.Add(trackingRow);
            header.Add(CreateSpacer(8f));
            header.Add(headlineLabel);
            header.Add(summaryLabel);
            header.Add(CreateCaptureProgressBar());
            header.Add(statusLabel);
            header.Add(CreateSpacer(6f));
            header.Add(skeletonStateLabel);
            header.Add(metricsLabel);
            header.Add(CreateSpacer(8f));
            header.Add(modeLabel);
            topRow.Add(header);

            settingsButton = CreateSecondaryButton(StudioL10n.ButtonSettings, () => actions.showSettings?.Invoke(), 42f);
            settingsButton.button.style.width = 96f;
            settingsButton.button.style.marginLeft = 10f;
            topRow.Add(settingsButton.button);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            Root.Add(spacer);

            var footer = BuildFooter();
            Root.Add(footer);
        }

        public void Render(CaptureScreenSnapshot snapshot)
        {
            headlineLabel.text = snapshot.headline;
            summaryLabel.text = snapshot.summary;
            modeLabel.text = snapshot.modeLabel;
            statusLabel.text = snapshot.status;
            metricsLabel.text = snapshot.metrics;
            skeletonStateLabel.text = StudioL10n.SkeletonState(snapshot.skeletonVisible);
            trackingSignalLabel.text = snapshot.trackingSignal;
            trackingDot.style.backgroundColor = snapshot.trackingSignalColor;
            progressFill.style.width = new Length(Mathf.Clamp01(snapshot.progress) * 100f, LengthUnit.Percent);
            footerLabel.text = snapshot.hasSelectedClip
                ? StudioL10n.CaptureLatestClipReady
                : StudioL10n.CapturePromptToRecord;

            stageButton.button.SetEnabled(snapshot.hasSelectedClip && !snapshot.isRecording);
            settingsButton.button.SetEnabled(!snapshot.isRecording);
            recordButton.button.SetEnabled(!snapshot.isRecording);
            stopButton.button.SetEnabled(snapshot.isRecording);
        }

        public void RefreshLocalizedChrome()
        {
            libraryButton.label.text = StudioL10n.ButtonLibrary;
            stageButton.label.text = StudioL10n.ButtonStage;
            settingsButton.label.text = StudioL10n.ButtonSettings;
            recordButton.label.text = StudioL10n.ButtonRecord;
            stopButton.label.text = StudioL10n.ButtonStop;
        }

        private VisualElement BuildFooter()
        {
            var footer = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var quickActions = CreateRow();
            libraryButton = CreateSecondaryButton(StudioL10n.ButtonLibrary, () => actions.showLibrary?.Invoke());
            libraryButton.button.style.marginRight = 10f;
            quickActions.Add(libraryButton.button);
            stageButton = CreateSecondaryButton(StudioL10n.ButtonStage, () => actions.showStage?.Invoke());
            quickActions.Add(stageButton.button);
            footer.Add(quickActions);
            footer.Add(CreateSpacer(14f));

            var recordRow = CreateRow();
            recordRow.style.justifyContent = Justify.Center;
            recordRow.style.alignItems = Align.Center;
            recordButton = CreatePrimaryButton(StudioL10n.ButtonRecord, () => actions.startRecording?.Invoke(), 54f);
            recordButton.button.style.minWidth = 132f;
            recordButton.button.style.marginRight = 18f;
            stopButton = CreateDangerButton(StudioL10n.ButtonStop, () => actions.stopRecording?.Invoke(), 54f);
            stopButton.button.style.minWidth = 132f;
            recordRow.Add(recordButton.button);
            recordRow.Add(stopButton.button);
            footer.Add(recordRow);
            footer.Add(CreateSpacer(12f));

            footerLabel = CreateCaptionLabel(string.Empty);
            footerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            footer.Add(footerLabel);

            return footer;
        }

        private VisualElement CreateTrackingDot()
        {
            var dot = new VisualElement();
            dot.style.width = 12f;
            dot.style.height = 12f;
            dot.style.marginRight = 8f;
            dot.style.borderTopLeftRadius = 999f;
            dot.style.borderTopRightRadius = 999f;
            dot.style.borderBottomLeftRadius = 999f;
            dot.style.borderBottomRightRadius = 999f;
            return dot;
        }

        private VisualElement CreateCaptureProgressBar()
        {
            var progressBar = new VisualElement();
            progressBar.style.height = 5f;
            progressBar.style.marginTop = 10f;
            progressBar.style.marginBottom = 8f;
            progressBar.style.backgroundColor = new Color(1f, 1f, 1f, 0.16f);
            progressBar.style.borderTopLeftRadius = 999f;
            progressBar.style.borderTopRightRadius = 999f;
            progressBar.style.borderBottomLeftRadius = 999f;
            progressBar.style.borderBottomRightRadius = 999f;

            progressFill = new VisualElement();
            progressFill.style.height = new Length(100f, LengthUnit.Percent);
            progressFill.style.width = new Length(0f, LengthUnit.Percent);
            progressFill.style.backgroundColor = new Color(0.92f, 0.14f, 0.18f);
            progressFill.style.borderTopLeftRadius = 999f;
            progressFill.style.borderTopRightRadius = 999f;
            progressFill.style.borderBottomLeftRadius = 999f;
            progressFill.style.borderBottomRightRadius = 999f;
            progressBar.Add(progressFill);

            return progressBar;
        }
    }
}
