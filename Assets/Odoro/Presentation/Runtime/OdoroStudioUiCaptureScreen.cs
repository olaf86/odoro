using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed partial class OdoroStudioUiToolkitView
    {
        private void BuildCaptureScreen()
        {
            captureScreen = CreateScreen("capture-screen");
            contentColumn.Add(captureScreen);

            var captureTopRow = CreateRow();
            captureTopRow.style.alignItems = Align.FlexStart;
            captureTopRow.style.justifyContent = Justify.SpaceBetween;
            captureTopRow.style.marginTop = 2f;
            captureScreen.Add(captureTopRow);

            var captureHeader = CreatePanel(new Color(0.14f, 0.17f, 0.22f, 0.84f), 22f);
            captureHeader.style.width = 224f;
            captureHeadlineLabel = CreateTitleLabel();
            captureSummaryLabel = CreateSubtitleLabel();
            captureModeLabel = CreatePillLabel();
            captureStatusLabel = CreateBodyLabel();
            captureMetricsLabel = CreateCaptionLabel(string.Empty);
            captureSkeletonStateLabel = CreateCaptionLabel(string.Empty);
            captureTrackingSignalLabel = CreateValueLabel();
            captureTrackingDot = CreateTrackingDot();

            var captureTrackingRow = CreateRow();
            captureTrackingRow.style.alignItems = Align.Center;
            captureTrackingRow.Add(captureTrackingDot);
            captureTrackingRow.Add(captureTrackingSignalLabel);

            var captureProgressBar = CreateCaptureProgressBar();
            captureHeader.Add(captureTrackingRow);
            captureHeader.Add(CreateSpacer(8f));
            captureHeader.Add(captureHeadlineLabel);
            captureHeader.Add(captureSummaryLabel);
            captureHeader.Add(captureProgressBar);
            captureHeader.Add(captureStatusLabel);
            captureHeader.Add(CreateSpacer(6f));
            captureHeader.Add(captureSkeletonStateLabel);
            captureHeader.Add(captureMetricsLabel);
            captureHeader.Add(CreateSpacer(8f));
            captureHeader.Add(captureModeLabel);
            captureTopRow.Add(captureHeader);

            captureSettingsButton = CreateSecondaryButton(StudioL10n.ButtonSettings, () => actions.showSettings?.Invoke(), 42f);
            captureSettingsButton.button.style.width = 96f;
            captureSettingsButton.button.style.marginLeft = 10f;
            captureTopRow.Add(captureSettingsButton.button);

            var captureSpacer = new VisualElement();
            captureSpacer.style.flexGrow = 1f;
            captureScreen.Add(captureSpacer);

            captureScreen.Add(BuildCaptureFooter());
        }

        private VisualElement BuildCaptureFooter()
        {
            var captureFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var captureQuickActions = CreateRow();
            captureLibraryButton = CreateSecondaryButton(StudioL10n.ButtonLibrary, () => actions.showLibrary?.Invoke());
            captureLibraryButton.button.style.marginRight = 10f;
            captureQuickActions.Add(captureLibraryButton.button);
            captureStageButton = CreateSecondaryButton(StudioL10n.ButtonStage, () => actions.showStage?.Invoke());
            captureQuickActions.Add(captureStageButton.button);
            captureFooter.Add(captureQuickActions);
            captureFooter.Add(CreateSpacer(14f));

            var recordRow = CreateRow();
            recordRow.style.justifyContent = Justify.Center;
            recordRow.style.alignItems = Align.Center;
            captureRecordButton = CreatePrimaryButton(StudioL10n.ButtonRecord, () => actions.startRecording?.Invoke(), 54f);
            captureRecordButton.button.style.minWidth = 132f;
            captureRecordButton.button.style.marginRight = 18f;
            captureStopButton = CreateDangerButton(StudioL10n.ButtonStop, () => actions.stopRecording?.Invoke(), 54f);
            captureStopButton.button.style.minWidth = 132f;
            recordRow.Add(captureRecordButton.button);
            recordRow.Add(captureStopButton.button);
            captureFooter.Add(recordRow);
            captureFooter.Add(CreateSpacer(12f));

            captureFooterLabel = CreateCaptionLabel(string.Empty);
            captureFooterLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            captureFooter.Add(captureFooterLabel);

            return captureFooter;
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

            captureProgressFill = new VisualElement();
            captureProgressFill.style.height = new Length(100f, LengthUnit.Percent);
            captureProgressFill.style.width = new Length(0f, LengthUnit.Percent);
            captureProgressFill.style.backgroundColor = new Color(0.92f, 0.14f, 0.18f);
            captureProgressFill.style.borderTopLeftRadius = 999f;
            captureProgressFill.style.borderTopRightRadius = 999f;
            captureProgressFill.style.borderBottomLeftRadius = 999f;
            captureProgressFill.style.borderBottomRightRadius = 999f;
            progressBar.Add(captureProgressFill);

            return progressBar;
        }
    }
}
