using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed partial class OdoroStudioUiToolkitView
    {
        private void BuildRecordingSettingsScreen()
        {
            settingsScreen = CreateScreen("settings-screen");
            contentColumn.Add(settingsScreen);

            var sessionPanel = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.92f), 22f);
            sessionTitleLabel = CreateSectionLabel(StudioL10n.RecordingSessionTitle);
            sessionSummaryLabel = CreateSubtitleLabel(StudioL10n.RecordingSessionHint);
            settingsBackButton = CreateSecondaryButton(StudioL10n.ButtonDone, () => actions.showCapture?.Invoke());
            settingsBackButton.button.style.width = 96f;
            settingsBackButton.button.style.marginBottom = 12f;
            sessionPanel.Add(settingsBackButton.button);
            sessionPanel.Add(sessionTitleLabel);
            sessionPanel.Add(sessionSummaryLabel);

            bpmStepper = AddStepper(sessionPanel, StudioL10n.SessionBpmTitle, actions.decreaseBpm, actions.increaseBpm);
            numeratorStepper = AddStepper(sessionPanel, StudioL10n.SessionNumeratorTitle, actions.decreaseNumerator, actions.increaseNumerator);
            denominatorStepper = AddStepper(sessionPanel, StudioL10n.SessionDenominatorTitle, actions.decreaseDenominator, actions.increaseDenominator);
            countInStepper = AddStepper(sessionPanel, StudioL10n.SessionCountInTitle, actions.decreaseCountInBars, actions.increaseCountInBars);
            sessionPanel.Add(BuildFixedDurationCard());

            settingsSkeletonButton = CreateSecondaryButton(StudioL10n.ButtonSkeletonOn, () => actions.toggleSkeleton?.Invoke(), 44f);
            settingsSkeletonButton.button.style.marginTop = 10f;
            sessionPanel.Add(settingsSkeletonButton.button);
            settingsScreen.Add(sessionPanel);
        }

        private VisualElement BuildFixedDurationCard()
        {
            var durationCard = CreateCard();
            durationCard.style.marginTop = 10f;
            fixedDurationTitleLabel = CreateCaptionLabel(StudioL10n.FixedCaptureDurationTitle);
            fixedDurationLabel = CreateValueLabel();
            durationCard.Add(fixedDurationTitleLabel);
            durationCard.Add(fixedDurationLabel);
            return durationCard;
        }
    }
}
