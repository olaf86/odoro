using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed class OdoroStudioUiActions
    {
        public Action showCapture;
        public Action showLibrary;
        public Action showStage;
        public Action startRecording;
        public Action stopRecording;
        public Action togglePlayback;
        public Action showModelInfo;
        public Action saveTake;
        public Action decreaseBpm;
        public Action increaseBpm;
        public Action decreaseNumerator;
        public Action increaseNumerator;
        public Action decreaseDenominator;
        public Action increaseDenominator;
        public Action decreaseCountInBars;
        public Action increaseCountInBars;
        public Action<MotionTakeSummary> openTake;
    }

    public sealed class OdoroStudioUiSnapshot
    {
        public StudioScreen screen;
        public MotionStudioState state;
        public MotionRecordingContext recordingContext;
        public CaptureMode captureMode;
        public MotionClip selectedClip;
        public MotionTakeSummary selectedTake;
        public IReadOnlyList<MotionTakeSummary> libraryClips;
        public bool hasAvatar;
        public string transientMessage;
        public string captureHeadline;
        public string captureSummary;
        public string captureStatus;
        public string captureModeLabel;
        public string stageTitle;
        public string stageSummary;
        public string stageModeLabel;
        public string stageHint;
    }

    public sealed class OdoroStudioUiToolkitView : IDisposable
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
        private readonly VisualElement captureScreen;
        private readonly VisualElement stageScreen;
        private readonly VisualElement libraryScreen;
        private readonly Label captureHeadlineLabel;
        private readonly Label captureSummaryLabel;
        private readonly Label captureModeLabel;
        private readonly Label captureStatusLabel;
        private readonly Label captureHintLabel;
        private readonly Label sessionTitleLabel;
        private readonly Label sessionSummaryLabel;
        private readonly StepperBinding bpmStepper;
        private readonly StepperBinding numeratorStepper;
        private readonly StepperBinding denominatorStepper;
        private readonly StepperBinding countInStepper;
        private readonly Label fixedDurationTitleLabel;
        private readonly Label fixedDurationLabel;
        private readonly Label captureFooterLabel;
        private readonly ButtonBinding captureStageButton;
        private readonly ButtonBinding captureRecordButton;
        private readonly ButtonBinding captureStopButton;
        private readonly Label stageTitleLabel;
        private readonly Label stageSummaryLabel;
        private readonly Label stageModeLabel;
        private readonly Label stageHintLabel;
        private readonly ButtonBinding stagePlaybackButton;
        private readonly ScrollView libraryScrollView;
        private readonly Label libraryTitleLabel;
        private readonly Label librarySubtitleLabel;
        private readonly Label libraryEmptyLabel;
        private readonly Label archiveRootLabel;
        private readonly Label toastLabel;
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

            captureScreen = CreateScreen("capture-screen");
            contentColumn.Add(captureScreen);

            var captureHeader = CreatePanel(new Color(0.14f, 0.17f, 0.22f, 0.84f), 22f);
            captureHeadlineLabel = CreateTitleLabel();
            captureSummaryLabel = CreateSubtitleLabel();
            captureModeLabel = CreatePillLabel();
            captureStatusLabel = CreateBodyLabel();
            captureHintLabel = CreateCaptionLabel(StudioL10n.CaptureHint);

            captureHeader.Add(captureHeadlineLabel);
            captureHeader.Add(captureSummaryLabel);
            captureHeader.Add(CreateSpacer(10f));
            captureHeader.Add(captureModeLabel);
            captureHeader.Add(CreateSpacer(10f));
            captureHeader.Add(captureStatusLabel);
            captureHeader.Add(CreateSpacer(8f));
            captureHeader.Add(captureHintLabel);
            captureScreen.Add(captureHeader);

            var sessionPanel = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.92f), 22f);
            sessionPanel.style.marginTop = 12f;
            sessionTitleLabel = CreateSectionLabel(StudioL10n.RecordingSessionTitle);
            sessionSummaryLabel = CreateSubtitleLabel(StudioL10n.RecordingSessionHint);
            sessionPanel.Add(sessionTitleLabel);
            sessionPanel.Add(sessionSummaryLabel);

            bpmStepper = AddStepper(sessionPanel, StudioL10n.SessionBpmTitle, actions.decreaseBpm, actions.increaseBpm);
            numeratorStepper = AddStepper(sessionPanel, StudioL10n.SessionNumeratorTitle, actions.decreaseNumerator, actions.increaseNumerator);
            denominatorStepper = AddStepper(sessionPanel, StudioL10n.SessionDenominatorTitle, actions.decreaseDenominator, actions.increaseDenominator);
            countInStepper = AddStepper(sessionPanel, StudioL10n.SessionCountInTitle, actions.decreaseCountInBars, actions.increaseCountInBars);

            var durationCard = CreateCard();
            durationCard.style.marginTop = 10f;
            fixedDurationTitleLabel = CreateCaptionLabel(StudioL10n.FixedCaptureDurationTitle);
            fixedDurationLabel = CreateValueLabel();
            durationCard.Add(fixedDurationTitleLabel);
            durationCard.Add(fixedDurationLabel);
            sessionPanel.Add(durationCard);
            captureScreen.Add(sessionPanel);

            var captureSpacer = new VisualElement();
            captureSpacer.style.flexGrow = 1f;
            captureScreen.Add(captureSpacer);

            var captureFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var captureQuickActions = CreateRow();
            var libraryButton = CreateSecondaryButton(StudioL10n.ButtonLibrary, () => actions.showLibrary?.Invoke());
            libraryButton.button.style.marginRight = 10f;
            captureQuickActions.Add(libraryButton.button);
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
            captureScreen.Add(captureFooter);

            stageScreen = CreateScreen("stage-screen");
            contentColumn.Add(stageScreen);

            var stageHeader = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var stageBackButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
            stageBackButton.button.style.width = 84f;
            stageBackButton.button.style.marginBottom = 12f;
            stageHeader.Add(stageBackButton.button);

            stageTitleLabel = CreateTitleLabel();
            stageSummaryLabel = CreateSubtitleLabel();
            stageModeLabel = CreatePillLabel();
            stageHeader.Add(stageTitleLabel);
            stageHeader.Add(stageSummaryLabel);
            stageHeader.Add(CreateSpacer(10f));
            stageHeader.Add(stageModeLabel);
            stageScreen.Add(stageHeader);

            var stageSpacer = new VisualElement();
            stageSpacer.style.flexGrow = 1f;
            stageScreen.Add(stageSpacer);

            var stageFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var stageQuickActions = CreateRow();
            var modelButton = CreateSecondaryButton(StudioL10n.ButtonModel, () => actions.showModelInfo?.Invoke());
            modelButton.button.style.marginRight = 10f;
            stageQuickActions.Add(modelButton.button);
            var recordAgainButton = CreateSecondaryButton(StudioL10n.ButtonRecordAgain, () => actions.showCapture?.Invoke());
            stageQuickActions.Add(recordAgainButton.button);
            stageFooter.Add(stageQuickActions);
            stageFooter.Add(CreateSpacer(14f));

            var stageActionRow = CreateRow();
            stagePlaybackButton = CreateSecondaryButton(StudioL10n.ButtonPlay, () => actions.togglePlayback?.Invoke(), 46f);
            stagePlaybackButton.button.style.flexGrow = 1f;
            stagePlaybackButton.button.style.marginRight = 10f;
            var saveButton = CreatePrimaryButton(StudioL10n.ButtonSave, () => actions.saveTake?.Invoke(), 46f);
            saveButton.button.style.flexGrow = 1f;
            stageActionRow.Add(stagePlaybackButton.button);
            stageActionRow.Add(saveButton.button);
            stageFooter.Add(stageActionRow);
            stageFooter.Add(CreateSpacer(12f));

            stageHintLabel = CreateCaptionLabel(string.Empty);
            stageHintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            stageFooter.Add(stageHintLabel);
            stageScreen.Add(stageFooter);

            libraryScreen = CreateScreen("library-screen");
            contentColumn.Add(libraryScreen);

            var libraryShell = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.94f), 22f);
            libraryShell.style.flexGrow = 1f;
            var libraryHeaderRow = CreateRow();
            libraryHeaderRow.style.alignItems = Align.Center;
            var libraryBackButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
            libraryBackButton.button.style.width = 84f;
            libraryBackButton.button.style.marginRight = 10f;
            libraryHeaderRow.Add(libraryBackButton.button);

            var libraryTitleColumn = new VisualElement();
            libraryTitleColumn.style.flexGrow = 1f;
            libraryTitleLabel = CreateSectionLabel(StudioL10n.LibraryTitle);
            librarySubtitleLabel = CreateSubtitleLabel(StudioL10n.LibrarySubtitle);
            libraryTitleColumn.Add(libraryTitleLabel);
            libraryTitleColumn.Add(librarySubtitleLabel);
            libraryHeaderRow.Add(libraryTitleColumn);
            libraryShell.Add(libraryHeaderRow);
            libraryShell.Add(CreateSpacer(14f));

            libraryEmptyLabel = CreateValueLabel(StudioL10n.LibraryEmpty);
            libraryEmptyLabel.style.display = DisplayStyle.None;
            libraryShell.Add(libraryEmptyLabel);

            libraryScrollView = new ScrollView(ScrollViewMode.Vertical);
            libraryScrollView.style.flexGrow = 1f;
            libraryScrollView.style.marginTop = 4f;
            libraryShell.Add(libraryScrollView);
            libraryShell.Add(CreateSpacer(12f));

            archiveRootLabel = CreateCaptionLabel(StudioL10n.ArchiveRootCaption);
            libraryShell.Add(archiveRootLabel);
            libraryScreen.Add(libraryShell);

            toastLabel = CreateBodyLabel();
            toastLabel.name = "odoro-toast";
            toastLabel.style.position = Position.Absolute;
            toastLabel.style.left = 24f;
            toastLabel.style.right = 24f;
            toastLabel.style.bottom = 18f;
            toastLabel.style.paddingLeft = 14f;
            toastLabel.style.paddingRight = 14f;
            toastLabel.style.paddingTop = 10f;
            toastLabel.style.paddingBottom = 10f;
            toastLabel.style.backgroundColor = Palette.Toast;
            toastLabel.style.borderTopLeftRadius = 18f;
            toastLabel.style.borderTopRightRadius = 18f;
            toastLabel.style.borderBottomLeftRadius = 18f;
            toastLabel.style.borderBottomRightRadius = 18f;
            toastLabel.style.display = DisplayStyle.None;
            toastLabel.style.color = Color.white;
            toastLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            safeAreaRoot.Add(toastLabel);
        }

        public void Render(OdoroStudioUiSnapshot snapshot)
        {
            captureScreen.style.display = snapshot.screen == StudioScreen.Capture ? DisplayStyle.Flex : DisplayStyle.None;
            stageScreen.style.display = snapshot.screen == StudioScreen.Stage ? DisplayStyle.Flex : DisplayStyle.None;
            libraryScreen.style.display = snapshot.screen == StudioScreen.ClipsLibrary ? DisplayStyle.Flex : DisplayStyle.None;

            captureHeadlineLabel.text = snapshot.captureHeadline;
            captureSummaryLabel.text = snapshot.captureSummary;
            captureModeLabel.text = snapshot.captureModeLabel;
            captureStatusLabel.text = snapshot.captureStatus;
            sessionSummaryLabel.text = snapshot.captureSummary;
            bpmStepper.valueLabel.text = snapshot.recordingContext.bpm.ToString("0");
            numeratorStepper.valueLabel.text = snapshot.recordingContext.timeSignatureNumerator.ToString();
            denominatorStepper.valueLabel.text = snapshot.recordingContext.timeSignatureDenominator.ToString();
            countInStepper.valueLabel.text = snapshot.recordingContext.countInBarCount.ToString();
            fixedDurationLabel.text = $"{snapshot.recordingContext.FixedCaptureDuration:0.00} sec";
            captureFooterLabel.text = snapshot.selectedClip != null
                ? StudioL10n.CaptureLatestClipReady
                : StudioL10n.CapturePromptToRecord;

            captureStageButton.button.SetEnabled(snapshot.selectedClip != null && !snapshot.state.isRecording);
            captureRecordButton.button.SetEnabled(!snapshot.state.isRecording);
            captureStopButton.button.SetEnabled(snapshot.state.isRecording);

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

        private void RenderLibrary(IReadOnlyList<MotionTakeSummary> takes)
        {
            libraryScrollView.Clear();
            libraryEmptyLabel.style.display = takes.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (var index = 0; index < takes.Count; index += 1)
            {
                var take = takes[index];
                var card = CreateCard();
                card.style.marginBottom = 10f;
                card.Add(CreateValueLabel(take.DisplayName));
                card.Add(CreateCaptionLabel(take.SecondarySummary));

                var openButton = CreatePrimaryButton(StudioL10n.OpenPlayback, () => actions.openTake?.Invoke(take), 40f);
                openButton.button.style.marginTop = 10f;
                card.Add(openButton.button);
                libraryScrollView.Add(card);
            }
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
