using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class LibraryScreenView
    {
        public VisualElement Root { get; }

        private readonly OdoroStudioUiActions actions;
        private ScrollView scrollView;
        private ButtonBinding backButton;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label emptyLabel;
        private Label archiveRootLabel;

        public LibraryScreenView(VisualElement parent, OdoroStudioUiActions actions)
        {
            this.actions = actions;
            Root = CreateScreen("library-screen");
            parent.Add(Root);

            var shell = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.94f), 22f);
            shell.style.flexGrow = 1f;
            shell.Add(BuildHeader());
            shell.Add(CreateSpacer(14f));

            emptyLabel = CreateValueLabel(StudioL10n.LibraryEmpty);
            emptyLabel.style.display = DisplayStyle.None;
            shell.Add(emptyLabel);

            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1f;
            scrollView.style.marginTop = 4f;
            shell.Add(scrollView);
            shell.Add(CreateSpacer(12f));

            archiveRootLabel = CreateCaptionLabel(StudioL10n.ArchiveRootCaption);
            shell.Add(archiveRootLabel);
            Root.Add(shell);
        }

        public void Render(LibraryScreenSnapshot snapshot)
        {
            RenderLibrary(snapshot.clips ?? Array.Empty<MotionTakeSummary>());
        }

        public void RefreshLocalizedText()
        {
            backButton.label.text = StudioL10n.ButtonBack;
            titleLabel.text = StudioL10n.LibraryTitle;
            subtitleLabel.text = StudioL10n.LibrarySubtitle;
            emptyLabel.text = StudioL10n.LibraryEmpty;
            archiveRootLabel.text = StudioL10n.ArchiveRootCaption;
        }

        private VisualElement BuildHeader()
        {
            var headerRow = CreateRow();
            headerRow.style.alignItems = Align.Center;
            backButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
            backButton.button.style.width = 84f;
            backButton.button.style.marginRight = 10f;
            headerRow.Add(backButton.button);

            var titleColumn = new VisualElement();
            titleColumn.style.flexGrow = 1f;
            titleLabel = CreateSectionLabel(StudioL10n.LibraryTitle);
            subtitleLabel = CreateSubtitleLabel(StudioL10n.LibrarySubtitle);
            titleColumn.Add(titleLabel);
            titleColumn.Add(subtitleLabel);
            headerRow.Add(titleColumn);
            return headerRow;
        }

        private void RenderLibrary(IReadOnlyList<MotionTakeSummary> takes)
        {
            scrollView.Clear();
            emptyLabel.style.display = takes.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

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
                scrollView.Add(card);
            }
        }
    }
}
