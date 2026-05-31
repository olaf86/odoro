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
        private readonly List<VisualElement> optionCards = new List<VisualElement>();

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
            optionCards.Clear();

            if (snapshot.options == null)
            {
                return;
            }

            foreach (var option in snapshot.options)
            {
                optionList.Add(BuildOptionCard(option));
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

        private VisualElement BuildOptionCard(StageAvatarOptionSnapshot option)
        {
            var card = CreateCard();
            card.style.marginBottom = 10f;
            card.style.backgroundColor = option.isSelected
                ? new Color(0.18f, 0.52f, 0.87f, 0.92f)
                : new Color(0.11f, 0.14f, 0.19f, 0.95f);
            if (option.usesAvatar && !option.isInstalled)
            {
                card.style.opacity = 0.76f;
            }

            var title = CreateValueLabel(option.title);
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            card.Add(title);

            var subtitle = CreateCaptionLabel(option.subtitle);
            subtitle.style.marginTop = 6f;
            subtitle.style.unityTextAlign = TextAnchor.MiddleLeft;
            card.Add(subtitle);

            var status = CreatePillLabel();
            status.text = option.isSelected
                ? StudioL10n.ModelSelected
                : option.usesAvatar && !option.isInstalled
                    ? StudioL10n.ModelNotInstalled
                    : option.usesAvatar
                    ? StudioL10n.ModelTapToUse
                    : StudioL10n.ModelSkeletonPreview;
            status.style.marginTop = 10f;
            card.Add(status);

            var actionsRow = CreateRow();
            actionsRow.style.marginTop = 12f;
            actionsRow.style.flexWrap = Wrap.Wrap;

            if (option.canInstall)
            {
                var installButton = CreatePrimaryButton(StudioL10n.ButtonInstall, () => actions.installAvatarOption?.Invoke(option.id), 36f);
                installButton.button.style.marginRight = 8f;
                installButton.button.style.marginBottom = 8f;
                actionsRow.Add(installButton.button);
            }
            else
            {
                var selectButton = CreateSecondaryButton(StudioL10n.ButtonUse, () => actions.selectAvatarOption?.Invoke(option.id), 36f);
                selectButton.button.SetEnabled(!option.isSelected);
                selectButton.button.style.marginRight = 8f;
                selectButton.button.style.marginBottom = 8f;
                actionsRow.Add(selectButton.button);
            }

            if (option.canUninstall)
            {
                var uninstallButton = CreateDangerButton(StudioL10n.ButtonUninstall, () => actions.uninstallAvatarOption?.Invoke(option.id), 36f);
                uninstallButton.button.style.marginBottom = 8f;
                actionsRow.Add(uninstallButton.button);
            }

            card.Add(actionsRow);
            optionCards.Add(card);
            return card;
        }
    }
}
