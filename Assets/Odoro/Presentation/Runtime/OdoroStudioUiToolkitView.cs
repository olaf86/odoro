using System;
using System.Collections.Generic;
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
        private readonly ThemeStyleSheet fallbackThemeStyleSheet;
        private readonly VisualElement root;
        private readonly VisualElement safeAreaRoot;
        private readonly VisualElement contentColumn;
        private readonly CaptureScreenView captureScreen;
        private readonly RecordingSettingsScreenView settingsScreen;
        private readonly StageScreenView stageScreen;
        private readonly LibraryScreenView libraryScreen;
        private readonly OdoroScreenTransitionController transitionController;
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
            panelSettings.themeStyleSheet = ResolveRuntimeThemeStyleSheet(out fallbackThemeStyleSheet);
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
            transitionController = new OdoroScreenTransitionController(safeAreaRoot, new Dictionary<StudioScreen, VisualElement>
            {
                [StudioScreen.Capture] = captureScreen.Root,
                [StudioScreen.RecordingSettings] = settingsScreen.Root,
                [StudioScreen.Stage] = stageScreen.Root,
                [StudioScreen.ClipsLibrary] = libraryScreen.Root,
            }, OdoroScreenTransitionProfile.Default);
            toastView = new StudioToastView(safeAreaRoot);
        }

        public void Render(OdoroStudioUiSnapshot snapshot)
        {
            transitionController.Show(snapshot.screen);

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

            RefreshLocalizedText(snapshot.stage?.isPlaying ?? false);
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

            if (fallbackThemeStyleSheet != null)
            {
                UnityEngine.Object.Destroy(fallbackThemeStyleSheet);
            }
        }

        private void RefreshLocalizedText(bool stagePlaybackActive)
        {
            captureScreen.RefreshLocalizedText();
            settingsScreen.RefreshLocalizedText();
            stageScreen.RefreshLocalizedText(stagePlaybackActive);
            libraryScreen.RefreshLocalizedText();
        }

        private static ThemeStyleSheet ResolveRuntimeThemeStyleSheet(out ThemeStyleSheet fallbackTheme)
        {
            fallbackTheme = null;

#if UNITY_EDITOR
            var packageTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                "Packages/com.unity.dt.app-ui/PackageResources/Styles/Themes/App UI.tss"
            );
            if (packageTheme != null)
            {
                return packageTheme;
            }
#endif

            var resourceTheme = Resources.Load<ThemeStyleSheet>("Themes/App UI");
            if (resourceTheme != null)
            {
                return resourceTheme;
            }

            var loadedThemes = Resources.FindObjectsOfTypeAll<ThemeStyleSheet>();
            if (loadedThemes.Length > 0)
            {
                return loadedThemes[0];
            }

            fallbackTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            fallbackTheme.name = "Odoro Runtime Fallback Theme";
            return fallbackTheme;
        }
    }
}
