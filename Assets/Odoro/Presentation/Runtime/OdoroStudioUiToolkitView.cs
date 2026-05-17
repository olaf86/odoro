using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed partial class OdoroStudioUiToolkitView : IDisposable
    {
        private sealed class StepperBinding
        {
            public Label titleLabel;
            public Label valueLabel;
        }

        private sealed class ButtonBinding
        {
            public Button button;
            public Label label;
        }

        private const float ReferencePhoneWidth = 390f;
        private const float ReferencePhoneHeight = 844f;

        private static Font uiFont;

        private readonly UIDocument document;
        private readonly PanelSettings panelSettings;
        private readonly VisualElement root;
        private readonly VisualElement safeAreaRoot;
        private readonly VisualElement contentColumn;
        private VisualElement captureScreen;
        private VisualElement settingsScreen;
        private VisualElement stageScreen;
        private VisualElement libraryScreen;
        private Label captureHeadlineLabel;
        private Label captureSummaryLabel;
        private Label captureModeLabel;
        private Label captureStatusLabel;
        private Label captureMetricsLabel;
        private Label captureSkeletonStateLabel;
        private Label captureTrackingSignalLabel;
        private VisualElement captureTrackingDot;
        private VisualElement captureProgressFill;
        private Label sessionTitleLabel;
        private Label sessionSummaryLabel;
        private StepperBinding bpmStepper;
        private StepperBinding numeratorStepper;
        private StepperBinding denominatorStepper;
        private StepperBinding countInStepper;
        private Label fixedDurationTitleLabel;
        private Label fixedDurationLabel;
        private Label captureFooterLabel;
        private ButtonBinding captureLibraryButton;
        private ButtonBinding captureStageButton;
        private ButtonBinding captureSettingsButton;
        private ButtonBinding captureRecordButton;
        private ButtonBinding captureStopButton;
        private ButtonBinding settingsBackButton;
        private ButtonBinding settingsSkeletonButton;
        private ButtonBinding stageBackButton;
        private Label stageTitleLabel;
        private Label stageSummaryLabel;
        private Label stageModeLabel;
        private Label stageHintLabel;
        private ButtonBinding stageModelButton;
        private ButtonBinding stageRecordAgainButton;
        private ButtonBinding stagePlaybackButton;
        private ButtonBinding stageSaveButton;
        private ScrollView libraryScrollView;
        private ButtonBinding libraryBackButton;
        private Label libraryTitleLabel;
        private Label librarySubtitleLabel;
        private Label libraryEmptyLabel;
        private Label archiveRootLabel;
        private Label toastLabel;
        private readonly OdoroStudioUiActions actions;

        public OdoroStudioUiToolkitView(GameObject host, OdoroStudioUiActions actions)
        {
            this.actions = actions;

            document = host.GetComponent<UIDocument>() ?? host.AddComponent<UIDocument>();
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Odoro Runtime Panel Settings";
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int((int)ReferencePhoneWidth, (int)ReferencePhoneHeight);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 100;
            document.panelSettings = panelSettings;
            document.sortingOrder = 100;

            root = document.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = Color.clear;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;

            safeAreaRoot = new VisualElement { name = "odoro-safe-area" };
            safeAreaRoot.style.flexGrow = 1f;
            safeAreaRoot.style.width = new Length(100f, LengthUnit.Percent);
            safeAreaRoot.style.position = Position.Relative;
            safeAreaRoot.style.alignItems = Align.Center;
            root.Add(safeAreaRoot);

            contentColumn = new VisualElement { name = "odoro-content-column" };
            contentColumn.style.flexGrow = 1f;
            contentColumn.style.width = new Length(100f, LengthUnit.Percent);
            contentColumn.style.maxWidth = 460f;
            contentColumn.style.paddingLeft = 16f;
            contentColumn.style.paddingRight = 16f;
            contentColumn.style.paddingTop = 14f;
            contentColumn.style.paddingBottom = 14f;
            safeAreaRoot.Add(contentColumn);

            BuildCaptureScreen();
            BuildRecordingSettingsScreen();
            BuildStageScreen();
            BuildLibraryScreen();
            BuildToast();
        }

        public void Render(OdoroStudioUiSnapshot snapshot)
        {
            RefreshLocalizedChrome(snapshot);
            captureScreen.style.display = snapshot.screen == StudioScreen.Capture ? DisplayStyle.Flex : DisplayStyle.None;
            settingsScreen.style.display = snapshot.screen == StudioScreen.RecordingSettings ? DisplayStyle.Flex : DisplayStyle.None;
            stageScreen.style.display = snapshot.screen == StudioScreen.Stage ? DisplayStyle.Flex : DisplayStyle.None;
            libraryScreen.style.display = snapshot.screen == StudioScreen.ClipsLibrary ? DisplayStyle.Flex : DisplayStyle.None;

            captureHeadlineLabel.text = snapshot.captureHeadline;
            captureSummaryLabel.text = snapshot.captureSummary;
            captureModeLabel.text = snapshot.captureModeLabel;
            captureStatusLabel.text = snapshot.captureStatus;
            captureMetricsLabel.text = snapshot.captureMetrics;
            captureSkeletonStateLabel.text = StudioL10n.SkeletonState(snapshot.captureSkeletonVisible);
            captureTrackingSignalLabel.text = snapshot.captureTrackingSignal;
            captureTrackingDot.style.backgroundColor = snapshot.captureTrackingSignalColor;
            captureProgressFill.style.width = new Length(Mathf.Clamp01(snapshot.captureProgress) * 100f, LengthUnit.Percent);
            sessionSummaryLabel.text = StudioL10n.RecordingSessionSummary(
                Mathf.RoundToInt(snapshot.recordingContext.bpm),
                snapshot.recordingContext.timeSignatureNumerator,
                snapshot.recordingContext.timeSignatureDenominator,
                snapshot.recordingContext.countInBarCount
            );
            bpmStepper.valueLabel.text = snapshot.recordingContext.bpm.ToString("0");
            numeratorStepper.valueLabel.text = snapshot.recordingContext.timeSignatureNumerator.ToString();
            denominatorStepper.valueLabel.text = snapshot.recordingContext.timeSignatureDenominator.ToString();
            countInStepper.valueLabel.text = snapshot.recordingContext.countInBarCount.ToString();
            fixedDurationLabel.text = StudioL10n.FixedDurationSeconds(snapshot.recordingContext.FixedCaptureDuration);
            captureFooterLabel.text = snapshot.selectedClip != null
                ? StudioL10n.CaptureLatestClipReady
                : StudioL10n.CapturePromptToRecord;

            captureStageButton.button.SetEnabled(snapshot.selectedClip != null && !snapshot.state.isRecording);
            captureSettingsButton.button.SetEnabled(!snapshot.state.isRecording);
            captureRecordButton.button.SetEnabled(!snapshot.state.isRecording);
            captureStopButton.button.SetEnabled(snapshot.state.isRecording);
            settingsSkeletonButton.label.text = snapshot.captureSkeletonVisible
                ? StudioL10n.ButtonSkeletonOff
                : StudioL10n.ButtonSkeletonOn;

            stageTitleLabel.text = snapshot.stageTitle;
            stageSummaryLabel.text = snapshot.stageSummary;
            stageModeLabel.text = snapshot.stageModeLabel;
            stageHintLabel.text = snapshot.stageHint;
            stagePlaybackButton.label.text = snapshot.state.isPlaying ? StudioL10n.ButtonPause : StudioL10n.ButtonPlay;
            stagePlaybackButton.button.SetEnabled(snapshot.selectedClip != null);

            RenderLibrary(snapshot.libraryClips ?? Array.Empty<MotionTakeSummary>());

            if (string.IsNullOrEmpty(snapshot.transientMessage))
            {
                toastLabel.style.display = DisplayStyle.None;
            }
            else
            {
                toastLabel.text = snapshot.transientMessage;
                toastLabel.style.display = DisplayStyle.Flex;
            }
        }

        public void ApplySafeArea(Rect safeArea)
        {
            var width = root.layout.width;
            var height = root.layout.height;
            if (width <= 0f || height <= 0f || Screen.width <= 0f || Screen.height <= 0f)
            {
                return;
            }

            var left = safeArea.xMin / Screen.width * width;
            var right = (Screen.width - safeArea.xMax) / Screen.width * width;
            var top = (Screen.height - safeArea.yMax) / Screen.height * height;
            var bottom = safeArea.yMin / Screen.height * height;

            safeAreaRoot.style.paddingLeft = left;
            safeAreaRoot.style.paddingRight = right;
            safeAreaRoot.style.paddingTop = top;
            safeAreaRoot.style.paddingBottom = bottom;
        }

        public void Dispose()
        {
            if (document != null)
            {
                document.panelSettings = null;
            }

            if (panelSettings != null)
            {
                UnityEngine.Object.Destroy(panelSettings);
            }
        }

        private void RefreshLocalizedChrome(OdoroStudioUiSnapshot snapshot)
        {
            sessionTitleLabel.text = StudioL10n.RecordingSessionTitle;
            bpmStepper.titleLabel.text = StudioL10n.SessionBpmTitle;
            numeratorStepper.titleLabel.text = StudioL10n.SessionNumeratorTitle;
            denominatorStepper.titleLabel.text = StudioL10n.SessionDenominatorTitle;
            countInStepper.titleLabel.text = StudioL10n.SessionCountInTitle;
            fixedDurationTitleLabel.text = StudioL10n.FixedCaptureDurationTitle;
            captureLibraryButton.label.text = StudioL10n.ButtonLibrary;
            captureStageButton.label.text = StudioL10n.ButtonStage;
            captureSettingsButton.label.text = StudioL10n.ButtonSettings;
            captureRecordButton.label.text = StudioL10n.ButtonRecord;
            captureStopButton.label.text = StudioL10n.ButtonStop;
            settingsBackButton.label.text = StudioL10n.ButtonDone;
            stageBackButton.label.text = StudioL10n.ButtonBack;
            stageModelButton.label.text = StudioL10n.ButtonModel;
            stageRecordAgainButton.label.text = StudioL10n.ButtonRecordAgain;
            stagePlaybackButton.label.text = snapshot.state.isPlaying ? StudioL10n.ButtonPause : StudioL10n.ButtonPlay;
            stageSaveButton.label.text = StudioL10n.ButtonSave;
            libraryBackButton.label.text = StudioL10n.ButtonBack;
            libraryTitleLabel.text = StudioL10n.LibraryTitle;
            librarySubtitleLabel.text = StudioL10n.LibrarySubtitle;
            libraryEmptyLabel.text = StudioL10n.LibraryEmpty;
            archiveRootLabel.text = StudioL10n.ArchiveRootCaption;
        }

        private StepperBinding AddStepper(VisualElement parent, string title, Action decrease, Action increase)
        {
            var card = CreateCard();
            card.style.marginTop = 10f;
            var titleLabel = CreateCaptionLabel(title);
            card.Add(titleLabel);

            var row = CreateRow();
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 8f;

            var decreaseButton = CreateSecondaryButton("-", decrease, 34f);
            decreaseButton.button.style.width = 44f;
            decreaseButton.button.style.marginRight = 8f;
            row.Add(decreaseButton.button);

            var valueLabel = CreateValueLabel("--");
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.flexGrow = 1f;
            row.Add(valueLabel);

            var increaseButton = CreateSecondaryButton("+", increase, 34f);
            increaseButton.button.style.width = 44f;
            increaseButton.button.style.marginLeft = 8f;
            row.Add(increaseButton.button);
            card.Add(row);
            parent.Add(card);

            return new StepperBinding
            {
                titleLabel = titleLabel,
                valueLabel = valueLabel,
            };
        }

        private static VisualElement CreateScreen(string name)
        {
            var screen = new VisualElement { name = name };
            screen.style.flexGrow = 1f;
            screen.style.flexDirection = FlexDirection.Column;
            return screen;
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
        }

        private static VisualElement CreateSpacer(float height)
        {
            var spacer = new VisualElement();
            spacer.style.height = height;
            return spacer;
        }

        private static VisualElement CreatePanel(Color color, float radius)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = color;
            panel.style.borderTopLeftRadius = radius;
            panel.style.borderTopRightRadius = radius;
            panel.style.borderBottomLeftRadius = radius;
            panel.style.borderBottomRightRadius = radius;
            panel.style.paddingLeft = 18f;
            panel.style.paddingRight = 18f;
            panel.style.paddingTop = 18f;
            panel.style.paddingBottom = 18f;
            return panel;
        }

        private static VisualElement CreateCard()
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 0.95f);
            card.style.borderTopLeftRadius = 18f;
            card.style.borderTopRightRadius = 18f;
            card.style.borderBottomLeftRadius = 18f;
            card.style.borderBottomRightRadius = 18f;
            card.style.paddingLeft = 14f;
            card.style.paddingRight = 14f;
            card.style.paddingTop = 12f;
            card.style.paddingBottom = 12f;
            return card;
        }

        private static Label CreateTitleLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 21f, FontStyle.Bold, Color.white);
            return label;
        }

        private static Label CreateSectionLabel(string text)
        {
            var label = new Label(text);
            ApplyTextStyle(label, 18f, FontStyle.Bold, Color.white);
            return label;
        }

        private static Label CreateSubtitleLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 12f, FontStyle.Normal, new Color(0.74f, 0.80f, 0.86f));
            return label;
        }

        private static Label CreateBodyLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 13f, FontStyle.Normal, new Color(0.86f, 0.89f, 0.92f));
            return label;
        }

        private static Label CreateCaptionLabel(string text)
        {
            var label = new Label(text);
            ApplyTextStyle(label, 11f, FontStyle.Normal, new Color(0.72f, 0.78f, 0.84f));
            return label;
        }

        private static Label CreateValueLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 15f, FontStyle.Bold, Color.white);
            return label;
        }

        private static Label CreatePillLabel()
        {
            var label = new Label();
            ApplyTextStyle(label, 11f, FontStyle.Bold, Color.white);
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.30f);
            label.style.paddingLeft = 12f;
            label.style.paddingRight = 12f;
            label.style.paddingTop = 7f;
            label.style.paddingBottom = 7f;
            label.style.borderTopLeftRadius = 999f;
            label.style.borderTopRightRadius = 999f;
            label.style.borderBottomLeftRadius = 999f;
            label.style.borderBottomRightRadius = 999f;
            label.style.alignSelf = Align.FlexStart;
            return label;
        }

        private static ButtonBinding CreatePrimaryButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.18f, 0.52f, 0.87f), Color.white);
        }

        private static ButtonBinding CreateSecondaryButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.16f, 0.19f, 0.24f), Color.white);
        }

        private static ButtonBinding CreateDangerButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.86f, 0.19f, 0.22f), Color.white);
        }

        private static ButtonBinding CreateButton(string text, Action action, float height, Color background, Color foreground)
        {
            var button = new Button(() => action?.Invoke());
            button.focusable = false;
            button.style.height = height;
            button.style.borderTopLeftRadius = 18f;
            button.style.borderTopRightRadius = 18f;
            button.style.borderBottomLeftRadius = 18f;
            button.style.borderBottomRightRadius = 18f;
            button.style.backgroundColor = background;
            button.style.paddingLeft = 16f;
            button.style.paddingRight = 16f;
            button.style.paddingTop = 10f;
            button.style.paddingBottom = 10f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;

            var label = new Label(text);
            ApplyTextStyle(label, 14f, FontStyle.Bold, foreground);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore;
            button.Add(label);

            return new ButtonBinding
            {
                button = button,
                label = label,
            };
        }

        private static void ApplyTextStyle(TextElement element, float fontSize, FontStyle fontStyle, Color color)
        {
            element.style.unityFont = uiFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            element.style.fontSize = fontSize;
            element.style.unityFontStyleAndWeight = fontStyle;
            element.style.color = color;
            element.style.whiteSpace = WhiteSpace.Normal;
        }
    }
}
