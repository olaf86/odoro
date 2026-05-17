using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed partial class OdoroStudioUiToolkitView
    {
        private void BuildLibraryScreen()
        {
            libraryScreen = CreateScreen("library-screen");
            contentColumn.Add(libraryScreen);

            var libraryShell = CreatePanel(new Color(0.08f, 0.10f, 0.14f, 0.94f), 22f);
            libraryShell.style.flexGrow = 1f;
            libraryShell.Add(BuildLibraryHeader());
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
        }

        private VisualElement BuildLibraryHeader()
        {
            var libraryHeaderRow = CreateRow();
            libraryHeaderRow.style.alignItems = Align.Center;
            libraryBackButton = CreateSecondaryButton(StudioL10n.ButtonBack, () => actions.showCapture?.Invoke());
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
            return libraryHeaderRow;
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
    }
}
