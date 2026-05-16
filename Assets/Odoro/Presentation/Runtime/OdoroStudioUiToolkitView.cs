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
            public Label valueLabel;
        }

        private const float ReferencePhoneWidth = 390f;
        private const float ReferencePhoneHeight = 844f;

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
        private readonly Label sessionSummaryLabel;
        private readonly Label captureFooterLabel;
        private readonly Button captureStageButton;
        private readonly Button captureRecordButton;
        private readonly Button captureStopButton;
        private readonly Foldout sessionFoldout;
        private readonly StepperBinding bpmStepper;
        private readonly StepperBinding numeratorStepper;
        private readonly StepperBinding denominatorStepper;
        private readonly StepperBinding countInStepper;
        private readonly Label fixedDurationLabel;
        private readonly Label stageTitleLabel;
        private readonly Label stageSummaryLabel;
        private readonly Label stageModeLabel;
        private readonly Label stageHintLabel;
        private readonly Button stagePlaybackButton;
        private readonly ScrollView libraryScrollView;
        private readonly Label libraryEmptyLabel;
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

            safeAreaRoot = new VisualElement
            {
                name = "odoro-safe-area",
            };
            safeAreaRoot.style.flexGrow = 1f;
            safeAreaRoot.style.width = new Length(100f, LengthUnit.Percent);
            safeAreaRoot.style.position = Position.Relative;
            safeAreaRoot.style.alignItems = Align.Center;
            root.Add(safeAreaRoot);

            contentColumn = new VisualElement
            {
                name = "odoro-content-column",
            };
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

            var captureHeader = CreatePanel(new Color(0.14f, 0.17f, 0.22f, 0.82f), 22f);
            captureHeadlineLabel = CreateTitleLabel();
            captureSummaryLabel = CreateSubtitleLabel();
            captureModeLabel = CreatePillLabel();
            captureStatusLabel = CreateBodyLabel();

            captureHeader.Add(captureHeadlineLabel);
            captureHeader.Add(captureSummaryLabel);
            captureHeader.Add(CreateSpacer(10f));

            var captureHeaderRow = CreateRow();
            captureHeaderRow.style.justifyContent = Justify.SpaceBetween;
            captureHeaderRow.Add(captureModeLabel);
            captureHeader.Add(captureHeaderRow);
            captureHeader.Add(CreateSpacer(10f));
            captureHeader.Add(captureStatusLabel);
            captureScreen.Add(captureHeader);

            sessionFoldout = new Foldout
            {
                text = "Session Settings",
                value = false,
            };
            sessionFoldout.style.marginTop = 12f;
            sessionFoldout.style.marginBottom = 8f;
            sessionFoldout.style.paddingLeft = 16f;
            sessionFoldout.style.paddingRight = 16f;
            sessionFoldout.style.paddingTop = 12f;
            sessionFoldout.style.paddingBottom = 14f;
            sessionFoldout.style.borderTopLeftRadius = 22f;
            sessionFoldout.style.borderTopRightRadius = 22f;
            sessionFoldout.style.borderBottomLeftRadius = 22f;
            sessionFoldout.style.borderBottomRightRadius = 22f;
            sessionFoldout.style.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 0.90f);
            sessionFoldout.style.color = Color.white;

            sessionSummaryLabel = CreateSubtitleLabel();
            sessionSummaryLabel.style.marginBottom = 8f;
            sessionFoldout.Add(sessionSummaryLabel);

            bpmStepper = AddStepper(sessionFoldout, "BPM", actions.decreaseBpm, actions.increaseBpm);
            numeratorStepper = AddStepper(sessionFoldout, "Numerator", actions.decreaseNumerator, actions.increaseNumerator);
            denominatorStepper = AddStepper(sessionFoldout, "Denominator", actions.decreaseDenominator, actions.increaseDenominator);
            countInStepper = AddStepper(sessionFoldout, "Count In Bars", actions.decreaseCountInBars, actions.increaseCountInBars);

            var durationCard = CreateCard();
            durationCard.style.marginTop = 10f;
            durationCard.Add(CreateCaptionLabel("Fixed Capture Duration"));
            fixedDurationLabel = CreateValueLabel();
            durationCard.Add(fixedDurationLabel);
            sessionFoldout.Add(durationCard);
            captureScreen.Add(sessionFoldout);

            var captureSpacer = new VisualElement();
            captureSpacer.style.flexGrow = 1f;
            captureScreen.Add(captureSpacer);

            var captureFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.68f), 22f);
            var captureQuickActions = CreateRow();
            var libraryButton = CreateSecondaryButton("Library", () => actions.showLibrary?.Invoke());
            libraryButton.style.marginRight = 10f;
            captureQuickActions.Add(libraryButton);
            captureStageButton = CreateSecondaryButton("Stage", () => actions.showStage?.Invoke());
            captureQuickActions.Add(captureStageButton);
            captureFooter.Add(captureQuickActions);
            captureFooter.Add(CreateSpacer(14f));

            var recordRow = CreateRow();
            recordRow.style.justifyContent = Justify.Center;
            recordRow.style.alignItems = Align.Center;
            captureRecordButton = CreatePrimaryButton("Record", () => actions.startRecording?.Invoke(), 54f);
            captureRecordButton.style.minWidth = 132f;
            captureRecordButton.style.marginRight = 18f;
            captureStopButton = CreateDangerButton("Stop", () => actions.stopRecording?.Invoke(), 54f);
            captureStopButton.style.minWidth = 132f;
            recordRow.Add(captureRecordButton);
            recordRow.Add(captureStopButton);
            captureFooter.Add(recordRow);
            captureFooter.Add(CreateSpacer(12f));

            captureFooterLabel = CreateCaptionLabel(string.Empty);
            captureFooterLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            captureFooter.Add(captureFooterLabel);
            captureScreen.Add(captureFooter);

            stageScreen = CreateScreen("stage-screen");
            contentColumn.Add(stageScreen);

            var stageHeader = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.70f), 22f);
            var stageBackButton = CreateSecondaryButton("Back", () => actions.showCapture?.Invoke());
            stageBackButton.style.width = 72f;
            stageHeader.Add(stageBackButton);
            stageHeader.Add(CreateSpacer(12f));

            var stageHeaderRow = CreateRow();
            stageHeaderRow.style.alignItems = Align.FlexStart;
            stageHeaderRow.style.justifyContent = Justify.SpaceBetween;
            var stageLeft = new VisualElement();
            stageLeft.style.flexGrow = 1f;

            stageTitleLabel = CreateTitleLabel();
            stageSummaryLabel = CreateSubtitleLabel();
            stageModeLabel = CreatePillLabel();
            stageLeft.Add(stageTitleLabel);
            stageLeft.Add(stageSummaryLabel);
            stageHeaderRow.Add(stageLeft);
            stageHeaderRow.Add(stageModeLabel);
            stageHeader.Add(stageHeaderRow);
            stageScreen.Add(stageHeader);

            var stageSpacer = new VisualElement();
            stageSpacer.style.flexGrow = 1f;
            stageScreen.Add(stageSpacer);

            var stageFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.70f), 22f);
            var stageQuickActions = CreateRow();
            var modelButton = CreateSecondaryButton("Model", () => actions.showModelInfo?.Invoke());
            modelButton.style.marginRight = 10f;
            stageQuickActions.Add(modelButton);
            stageQuickActions.Add(CreateSecondaryButton("Record Again", () => actions.showCapture?.Invoke()));
            stageFooter.Add(stageQuickActions);
            stageFooter.Add(CreateSpacer(14f));

            var stageActionRow = CreateRow();
            stagePlaybackButton = CreateSecondaryButton("Play", () => actions.togglePlayback?.Invoke(), 46f);
            stagePlaybackButton.style.flexGrow = 1f;
            stagePlaybackButton.style.marginRight = 10f;
            var saveButton = CreatePrimaryButton("Saved", () => actions.saveTake?.Invoke(), 46f);
            saveButton.style.flexGrow = 1f;
            stageActionRow.Add(stagePlaybackButton);
            stageActionRow.Add(saveButton);
            stageFooter.Add(stageActionRow);
            stageFooter.Add(CreateSpacer(12f));

            stageHintLabel = CreateCaptionLabel(string.Empty);
            stageHintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            stageFooter.Add(stageHintLabel);
            stageScreen.Add(stageFooter);

            libraryScreen = CreateScreen("library-screen");
            contentColumn.Add(libraryScreen);

            var libraryShell = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.92f), 22f);
            libraryShell.style.flexGrow = 1f;
            libraryShell.style.marginBottom = 8f;
            var libraryHeaderRow = CreateRow();
            libraryHeaderRow.style.alignItems = Align.Center;
            var libraryBackButton = CreateSecondaryButton("Back", () => actions.showCapture?.Invoke());
            libraryBackButton.style.width = 72f;
            libraryBackButton.style.marginRight = 10f;
            libraryHeaderRow.Add(libraryBackButton);

            var libraryTitleColumn = new VisualElement();
            libraryTitleColumn.style.flexGrow = 1f;
            libraryTitleColumn.Add(CreateSectionLabel("Clip Library"));
            libraryTitleColumn.Add(CreateSubtitleLabel("Review past takes and jump back into playback."));
            libraryHeaderRow.Add(libraryTitleColumn);
            libraryShell.Add(libraryHeaderRow);
            libraryShell.Add(CreateSpacer(14f));

            libraryEmptyLabel = CreateValueLabel();
            libraryEmptyLabel.text = "No clips yet";
            libraryEmptyLabel.style.display = DisplayStyle.None;
            libraryShell.Add(libraryEmptyLabel);

            libraryScrollView = new ScrollView(ScrollViewMode.Vertical);
            libraryScrollView.style.flexGrow = 1f;
            libraryScrollView.style.marginTop = 4f;
            libraryShell.Add(libraryScrollView);
            libraryShell.Add(CreateSpacer(12f));
            libraryShell.Add(CreateCaptionLabel("Archive root: Application.persistentDataPath/OdoroArchiveV2"));
            libraryScreen.Add(libraryShell);

            toastLabel = new Label
            {
                name = "odoro-toast",
            };
            toastLabel.style.position = Position.Absolute;
            toastLabel.style.left = 24f;
            toastLabel.style.right = 24f;
            toastLabel.style.bottom = 18f;
            toastLabel.style.paddingLeft = 14f;
            toastLabel.style.paddingRight = 14f;
            toastLabel.style.paddingTop = 10f;
            toastLabel.style.paddingBottom = 10f;
            toastLabel.style.backgroundColor = Palette.Toast;
            toastLabel.style.color = Color.white;
            toastLabel.style.borderTopLeftRadius = 18f;
            toastLabel.style.borderTopRightRadius = 18f;
            toastLabel.style.borderBottomLeftRadius = 18f;
            toastLabel.style.borderBottomRightRadius = 18f;
            toastLabel.style.display = DisplayStyle.None;
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
                ? "Latest clip is ready. Open Stage to preview the take."
                : "Tap Record to capture the first take.";

            captureStageButton.SetEnabled(snapshot.selectedClip != null && !snapshot.state.isRecording);
            captureRecordButton.SetEnabled(!snapshot.state.isRecording);
            captureStopButton.SetEnabled(snapshot.state.isRecording);

            stageTitleLabel.text = snapshot.stageTitle;
            stageSummaryLabel.text = snapshot.stageSummary;
            stageModeLabel.text = snapshot.stageModeLabel;
            stageHintLabel.text = snapshot.stageHint;
            stagePlaybackButton.text = snapshot.state.isPlaying ? "Pause" : "Play";
            stagePlaybackButton.SetEnabled(snapshot.selectedClip != null);

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

                var openButton = CreatePrimaryButton("Open Playback", () => actions.openTake?.Invoke(take), 40f);
                openButton.style.marginTop = 10f;
                card.Add(openButton);
                libraryScrollView.Add(card);
            }
        }

        private StepperBinding AddStepper(VisualElement parent, string label, Action decrease, Action increase)
        {
            var card = CreateCard();
            card.style.marginTop = 10f;
            card.Add(CreateCaptionLabel(label));

            var row = CreateRow();
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 8f;

            var decreaseButton = CreateSecondaryButton("-", decrease, 34f);
            decreaseButton.style.width = 44f;
            row.Add(decreaseButton);

            var valueLabel = CreateValueLabel("--");
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.flexGrow = 1f;
            row.Add(valueLabel);

            var increaseButton = CreateSecondaryButton("+", increase, 34f);
            increaseButton.style.width = 44f;
            row.Add(increaseButton);
            card.Add(row);
            parent.Add(card);

            return new StepperBinding
            {
                valueLabel = valueLabel,
            };
        }

        private static VisualElement CreateScreen(string name)
        {
            var screen = new VisualElement
            {
                name = name,
            };
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
            label.style.color = Color.white;
            label.style.fontSize = 21f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateSectionLabel(string text)
        {
            var label = CreateTitleLabel(text);
            label.style.fontSize = 18f;
            return label;
        }

        private static Label CreateSubtitleLabel(string text = "")
        {
            var label = new Label(text);
            label.style.color = new Color(0.74f, 0.80f, 0.86f);
            label.style.fontSize = 12f;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateBodyLabel(string text = "")
        {
            var label = new Label(text);
            label.style.color = new Color(0.86f, 0.89f, 0.92f);
            label.style.fontSize = 13f;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateCaptionLabel(string text)
        {
            var label = new Label(text);
            label.style.color = new Color(0.72f, 0.78f, 0.84f);
            label.style.fontSize = 11f;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreateValueLabel(string text = "")
        {
            var label = new Label(text);
            label.style.color = Color.white;
            label.style.fontSize = 15f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Label CreatePillLabel()
        {
            var label = new Label();
            label.style.color = Color.white;
            label.style.fontSize = 11f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.30f);
            label.style.paddingLeft = 12f;
            label.style.paddingRight = 12f;
            label.style.paddingTop = 7f;
            label.style.paddingBottom = 7f;
            label.style.borderTopLeftRadius = 999f;
            label.style.borderTopRightRadius = 999f;
            label.style.borderBottomLeftRadius = 999f;
            label.style.borderBottomRightRadius = 999f;
            return label;
        }

        private static Button CreatePrimaryButton(string text, Action action, float height = 40f)
        {
            var button = new Button(() => action?.Invoke())
            {
                text = text,
            };
            StyleButton(button, new Color(0.18f, 0.52f, 0.87f), Color.white, height);
            return button;
        }

        private static Button CreateSecondaryButton(string text, Action action, float height = 40f)
        {
            var button = new Button(() => action?.Invoke())
            {
                text = text,
            };
            StyleButton(button, new Color(0.16f, 0.19f, 0.24f), Color.white, height);
            return button;
        }

        private static Button CreateDangerButton(string text, Action action, float height = 40f)
        {
            var button = new Button(() => action?.Invoke())
            {
                text = text,
            };
            StyleButton(button, new Color(0.86f, 0.19f, 0.22f), Color.white, height);
            return button;
        }

        private static void StyleButton(Button button, Color backgroundColor, Color textColor, float height)
        {
            button.style.height = height;
            button.style.borderTopLeftRadius = 18f;
            button.style.borderTopRightRadius = 18f;
            button.style.borderBottomLeftRadius = 18f;
            button.style.borderBottomRightRadius = 18f;
            button.style.backgroundColor = backgroundColor;
            button.style.color = textColor;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 14f;
            button.style.paddingLeft = 16f;
            button.style.paddingRight = 16f;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.flexGrow = 1f;
        }
    }
}
