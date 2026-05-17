using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed partial class OdoroStudioUiToolkitView
    {
        private void BuildStageScreen()
        {
            stageScreen = CreateScreen("stage-screen");
            contentColumn.Add(stageScreen);
            stageScreen.Add(BuildStageHeader());

            var stageSpacer = new VisualElement();
            stageSpacer.style.flexGrow = 1f;
            stageScreen.Add(stageSpacer);

            stageScreen.Add(BuildStageFooter());
        }

        private VisualElement BuildStageHeader()
        {
            var stageHeader = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            stageBackButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
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
            return stageHeader;
        }

        private VisualElement BuildStageFooter()
        {
            var stageFooter = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.72f), 22f);
            var stageQuickActions = CreateRow();
            stageModelButton = CreateSecondaryButton(StudioL10n.ButtonModel, () => actions.showModelInfo?.Invoke());
            stageModelButton.button.style.marginRight = 10f;
            stageQuickActions.Add(stageModelButton.button);
            stageRecordAgainButton = CreateSecondaryButton(StudioL10n.ButtonRecordAgain, () => actions.showCapture?.Invoke());
            stageQuickActions.Add(stageRecordAgainButton.button);
            stageFooter.Add(stageQuickActions);
            stageFooter.Add(CreateSpacer(14f));

            var stageActionRow = CreateRow();
            stagePlaybackButton = CreateSecondaryButton(StudioL10n.ButtonPlay, () => actions.togglePlayback?.Invoke(), 46f);
            stagePlaybackButton.button.style.flexGrow = 1f;
            stagePlaybackButton.button.style.marginRight = 10f;
            stageSaveButton = CreatePrimaryButton(StudioL10n.ButtonSave, () => actions.saveTake?.Invoke(), 46f);
            stageSaveButton.button.style.flexGrow = 1f;
            stageActionRow.Add(stagePlaybackButton.button);
            stageActionRow.Add(stageSaveButton.button);
            stageFooter.Add(stageActionRow);
            stageFooter.Add(CreateSpacer(12f));

            stageHintLabel = CreateCaptionLabel(string.Empty);
            stageHintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            stageFooter.Add(stageHintLabel);
            return stageFooter;
        }
    }
}
