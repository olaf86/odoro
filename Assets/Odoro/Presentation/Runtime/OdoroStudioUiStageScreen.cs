using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class StageScreenView
    {
        public VisualElement Root { get; }

        private readonly OdoroStudioUiActions actions;
        private ButtonBinding backButton;
        private Label titleLabel;
        private Label summaryLabel;
        private Label modeLabel;
        private Label hintLabel;
        private ButtonBinding modelButton;
        private ButtonBinding recordAgainButton;
        private ButtonBinding playbackButton;
        private ButtonBinding saveButton;

        public StageScreenView(VisualElement parent, OdoroStudioUiActions actions)
        {
            this.actions = actions;
            Root = CreateScreen("stage-screen");
            parent.Add(Root);
            Root.Add(BuildHeader());

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            Root.Add(spacer);

            Root.Add(BuildFooter());
        }

        public void Render(StageScreenSnapshot snapshot)
        {
            titleLabel.text = snapshot.title;
            summaryLabel.text = snapshot.summary;
            modeLabel.text = snapshot.modeLabel;
            hintLabel.text = snapshot.hint;
            playbackButton.label.text = snapshot.isPlaying ? StudioL10n.ButtonPause : StudioL10n.ButtonPlay;
            playbackButton.button.SetEnabled(snapshot.hasSelectedClip);
        }

        public void RefreshLocalizedChrome(bool isPlaying)
        {
            backButton.label.text = StudioL10n.ButtonBack;
            modelButton.label.text = StudioL10n.ButtonModel;
            recordAgainButton.label.text = StudioL10n.ButtonRecordAgain;
            playbackButton.label.text = isPlaying ? StudioL10n.ButtonPause : StudioL10n.ButtonPlay;
            saveButton.label.text = StudioL10n.ButtonSave;
        }

        private VisualElement BuildHeader()
        {
            var header = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            backButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
            backButton.button.style.width = 84f;
            backButton.button.style.marginBottom = 12f;
            header.Add(backButton.button);

            titleLabel = CreateTitleLabel();
            summaryLabel = CreateSubtitleLabel();
            modeLabel = CreatePillLabel();
            header.Add(titleLabel);
            header.Add(summaryLabel);
            header.Add(CreateSpacer(10f));
            header.Add(modeLabel);
            return header;
        }

        private VisualElement BuildFooter()
        {
            var footer = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var quickActions = CreateRow();
            modelButton = CreateSecondaryButton(StudioL10n.ButtonModel, () => actions.showModelInfo?.Invoke());
            modelButton.button.style.marginRight = 10f;
            quickActions.Add(modelButton.button);
            recordAgainButton = CreateSecondaryButton(StudioL10n.ButtonRecordAgain, () => actions.showCapture?.Invoke());
            quickActions.Add(recordAgainButton.button);
            footer.Add(quickActions);
            footer.Add(CreateSpacer(14f));

            var actionRow = CreateRow();
            playbackButton = CreateSecondaryButton(StudioL10n.ButtonPlay, () => actions.togglePlayback?.Invoke(), 46f);
            playbackButton.button.style.flexGrow = 1f;
            playbackButton.button.style.marginRight = 10f;
            saveButton = CreatePrimaryButton(StudioL10n.ButtonSave, () => actions.saveTake?.Invoke(), 46f);
            saveButton.button.style.flexGrow = 1f;
            actionRow.Add(playbackButton.button);
            actionRow.Add(saveButton.button);
            footer.Add(actionRow);
            footer.Add(CreateSpacer(12f));

            hintLabel = CreateCaptionLabel(string.Empty);
            hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            footer.Add(hintLabel);
            return footer;
        }
    }
}
