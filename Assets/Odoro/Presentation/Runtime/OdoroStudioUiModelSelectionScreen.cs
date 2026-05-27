using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class ModelSelectionScreenView
    {
        public VisualElement Root { get; }

        private readonly OdoroStudioUiActions actions;
        private readonly ScrollView optionList;
        private ButtonBinding backButton;
        private Label titleLabel;
        private Label subtitleLabel;
        private readonly List<Button> optionButtons = new List<Button>();

        public ModelSelectionScreenView(VisualElement parent, OdoroStudioUiActions actions)
        {
            this.actions = actions;
            Root = CreateScreen("model-selection-screen");
            parent.Add(Root);
            Root.Add(BuildHeader());

            optionList = new ScrollView(ScrollViewMode.Vertical);
            optionList.style.flexGrow = 1f;
            optionList.style.marginTop = 14f;
            Root.Add(optionList);
        }

        public void Render(ModelSelectionScreenSnapshot snapshot)
        {
            optionList.Clear();
            optionButtons.Clear();

            if (snapshot.options == null)
            {
                return;
            }

            foreach (var option in snapshot.options)
            {
                optionList.Add(BuildOptionButton(option, snapshot.isBusy));
            }
        }

        public void RefreshLocalizedText()
        {
            backButton.label.text = StudioL10n.ButtonBack;
            titleLabel.text = StudioL10n.ModelSelectionTitle;
            subtitleLabel.text = StudioL10n.ModelSelectionSubtitle;
        }

        private VisualElement BuildHeader()
        {
            var header = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);

            backButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showStage?.Invoke());
            backButton.button.style.width = 84f;
            header.Add(backButton.button);

            header.Add(CreateSpacer(14f));
            titleLabel = CreateTitleLabel(StudioL10n.ModelSelectionTitle);
            subtitleLabel = CreateSubtitleLabel(StudioL10n.ModelSelectionSubtitle);
            header.Add(titleLabel);
            header.Add(subtitleLabel);
            return header;
        }

        private Button BuildOptionButton(StageAvatarOptionSnapshot option, bool isBusy)
        {
            var button = new Button(() => actions.selectAvatarOption?.Invoke(option.id));
            button.focusable = false;
            button.SetEnabled(!isBusy);
            button.style.marginBottom = 10f;
            button.style.paddingLeft = 14f;
            button.style.paddingRight = 14f;
            button.style.paddingTop = 12f;
            button.style.paddingBottom = 12f;
            button.style.backgroundColor = option.isSelected
                ? new Color(0.18f, 0.52f, 0.87f, 0.92f)
                : new Color(0.11f, 0.14f, 0.19f, 0.95f);
            button.style.borderTopLeftRadius = 18f;
            button.style.borderTopRightRadius = 18f;
            button.style.borderBottomLeftRadius = 18f;
            button.style.borderBottomRightRadius = 18f;

            var title = CreateValueLabel(option.title);
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.Add(title);

            var subtitle = CreateCaptionLabel(option.subtitle);
            subtitle.style.marginTop = 6f;
            subtitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.Add(subtitle);

            var status = CreatePillLabel();
            status.text = option.isSelected
                ? StudioL10n.ModelSelected
                : option.requiresDownload
                    ? StudioL10n.ModelTapToDownload
                    : option.usesAvatar
                    ? StudioL10n.ModelTapToUse
                    : StudioL10n.ModelSkeletonPreview;
            status.style.marginTop = 10f;
            button.Add(status);

            optionButtons.Add(button);
            return button;
        }
    }
}
