using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed class OdoroStudioUiToolkitView : IDisposable
    {
        private const float ReferencePhoneWidth = 390f;
        private const float ReferencePhoneHeight = 844f;

        private readonly UIDocument document;
        private readonly PanelSettings panelSettings;
        private readonly VisualElement root;
        private readonly VisualElement safeAreaRoot;
        private readonly VisualElement contentColumn;
        private readonly CaptureScreenView captureScreen;
        private readonly RecordingSettingsScreenView settingsScreen;
        private readonly StageScreenView stageScreen;
        private readonly LibraryScreenView libraryScreen;
        private readonly StudioToastView toastView;

        public OdoroStudioUiToolkitView(GameObject host, OdoroStudioUiActions actions)
        {
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

            captureScreen = new CaptureScreenView(contentColumn, actions);
            settingsScreen = new RecordingSettingsScreenView(contentColumn, actions);
            stageScreen = new StageScreenView(contentColumn, actions);
            libraryScreen = new LibraryScreenView(contentColumn, actions);
            toastView = new StudioToastView(safeAreaRoot);
        }

        public void Render(OdoroStudioUiSnapshot snapshot)
        {
            SetActiveScreen(snapshot.screen);

            if (snapshot.capture != null)
            {
                captureScreen.Render(snapshot.capture);
            }

            if (snapshot.settings != null)
            {
                settingsScreen.Render(snapshot.settings);
            }

            if (snapshot.stage != null)
            {
                stageScreen.Render(snapshot.stage);
            }

            if (snapshot.library != null)
            {
                libraryScreen.Render(snapshot.library);
            }

            RefreshLocalizedChrome(snapshot.stage?.isPlaying ?? false);
            toastView.Render(snapshot.transientMessage);
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

        private void SetActiveScreen(StudioScreen screen)
        {
            captureScreen.Root.style.display = screen == StudioScreen.Capture ? DisplayStyle.Flex : DisplayStyle.None;
            settingsScreen.Root.style.display = screen == StudioScreen.RecordingSettings ? DisplayStyle.Flex : DisplayStyle.None;
            stageScreen.Root.style.display = screen == StudioScreen.Stage ? DisplayStyle.Flex : DisplayStyle.None;
            libraryScreen.Root.style.display = screen == StudioScreen.ClipsLibrary ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshLocalizedChrome(bool stagePlaybackActive)
        {
            captureScreen.RefreshLocalizedChrome();
            settingsScreen.RefreshLocalizedChrome();
            stageScreen.RefreshLocalizedChrome(stagePlaybackActive);
            libraryScreen.RefreshLocalizedChrome();
        }
    }
}
