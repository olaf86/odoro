using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class RecordingSettingsScreenView
    {
        public VisualElement Root { get; }

        private readonly Label sessionTitleLabel;
        private readonly Label sessionSummaryLabel;
        private readonly StepperBinding bpmStepper;
        private readonly StepperBinding numeratorStepper;
        private readonly StepperBinding denominatorStepper;
        private readonly StepperBinding countInStepper;
        private Label fixedDurationTitleLabel;
        private Label fixedDurationLabel;
        private readonly ButtonBinding backButton;
        private readonly ButtonBinding skeletonButton;

        public RecordingSettingsScreenView(VisualElement parent, OdoroStudioUiActions actions)
        {
            Root = CreateScreen("settings-screen");
            parent.Add(Root);

            var sessionPanel = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.92f), 22f);
            sessionTitleLabel = CreateSectionLabel(StudioL10n.RecordingSessionTitle);
            sessionSummaryLabel = CreateSubtitleLabel(StudioL10n.RecordingSessionHint);
            backButton = CreateSecondaryButton(StudioL10n.ButtonDone, () => actions.showCapture?.Invoke());
            backButton.button.style.width = 96f;
            backButton.button.style.marginBottom = 12f;
            sessionPanel.Add(backButton.button);
            sessionPanel.Add(sessionTitleLabel);
            sessionPanel.Add(sessionSummaryLabel);

            bpmStepper = AddStepper(sessionPanel, StudioL10n.SessionBpmTitle, actions.decreaseBpm, actions.increaseBpm);
            numeratorStepper = AddStepper(sessionPanel, StudioL10n.SessionNumeratorTitle, actions.decreaseNumerator, actions.increaseNumerator);
            denominatorStepper = AddStepper(sessionPanel, StudioL10n.SessionDenominatorTitle, actions.decreaseDenominator, actions.increaseDenominator);
            countInStepper = AddStepper(sessionPanel, StudioL10n.SessionCountInTitle, actions.decreaseCountInBars, actions.increaseCountInBars);
            sessionPanel.Add(BuildFixedDurationCard());

            skeletonButton = CreateSecondaryButton(StudioL10n.ButtonSkeletonOn, () => actions.toggleSkeleton?.Invoke(), 44f);
            skeletonButton.button.style.marginTop = 10f;
            sessionPanel.Add(skeletonButton.button);
            Root.Add(sessionPanel);
        }

        public void Render(RecordingSettingsScreenSnapshot snapshot)
        {
            sessionSummaryLabel.text = StudioL10n.RecordingSessionSummary(
                Mathf.RoundToInt(snapshot.bpm),
                snapshot.timeSignatureNumerator,
                snapshot.timeSignatureDenominator,
                snapshot.countInBarCount
            );
            bpmStepper.valueLabel.text = snapshot.bpm.ToString("0");
            numeratorStepper.valueLabel.text = snapshot.timeSignatureNumerator.ToString();
            denominatorStepper.valueLabel.text = snapshot.timeSignatureDenominator.ToString();
            countInStepper.valueLabel.text = snapshot.countInBarCount.ToString();
            fixedDurationLabel.text = StudioL10n.FixedDurationSeconds(snapshot.fixedCaptureDuration);
            skeletonButton.label.text = snapshot.skeletonVisible
                ? StudioL10n.ButtonSkeletonOff
                : StudioL10n.ButtonSkeletonOn;
        }

        public void RefreshLocalizedChrome()
        {
            sessionTitleLabel.text = StudioL10n.RecordingSessionTitle;
            bpmStepper.titleLabel.text = StudioL10n.SessionBpmTitle;
            numeratorStepper.titleLabel.text = StudioL10n.SessionNumeratorTitle;
            denominatorStepper.titleLabel.text = StudioL10n.SessionDenominatorTitle;
            countInStepper.titleLabel.text = StudioL10n.SessionCountInTitle;
            fixedDurationTitleLabel.text = StudioL10n.FixedCaptureDurationTitle;
            backButton.label.text = StudioL10n.ButtonDone;
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
