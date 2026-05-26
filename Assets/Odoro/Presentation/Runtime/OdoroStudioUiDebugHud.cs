using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class DebugHudView
    {
        private readonly OdoroStudioUiActions actions;
        private readonly Button collapsedButton;
        private readonly VisualElement panel;
        private readonly Label titleLabel;
        private readonly Label bodyLabel;
        private readonly Button hideButton;
        private readonly Button captureButton;
        private readonly Button shareButton;

        public DebugHudView(VisualElement parent, OdoroStudioUiActions actions)
        {
            this.actions = actions;

            collapsedButton = new Button(() => actions.showDebugHud?.Invoke())
            {
                text = "Debug",
                name = "odoro-debug-collapsed",
            };
            StyleButton(collapsedButton);
            collapsedButton.style.position = Position.Absolute;
            collapsedButton.style.left = 18f;
            collapsedButton.style.top = 18f;
            collapsedButton.style.width = 118f;
            collapsedButton.style.height = 52f;
            parent.Add(collapsedButton);

            panel = CreatePanel(new Color(0.04f, 0.05f, 0.08f, 0.90f), 18f);
            panel.name = "odoro-debug-hud";
            panel.style.position = Position.Absolute;
            panel.style.left = 18f;
            panel.style.top = 18f;
            panel.style.width = 350f;
            panel.style.maxHeight = 520f;

            var header = CreateRow();
            header.style.alignItems = Align.Center;
            titleLabel = CreateValueLabel("Odoro Debug HUD");
            titleLabel.style.flexGrow = 1f;
            titleLabel.style.fontSize = 18f;
            header.Add(titleLabel);

            hideButton = new Button(() => actions.hideDebugHud?.Invoke()) { text = "Hide" };
            StyleButton(hideButton);
            hideButton.style.width = 78f;
            hideButton.style.height = 44f;
            header.Add(hideButton);
            panel.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.maxHeight = 320f;
            scroll.style.marginTop = 10f;
            bodyLabel = CreateBodyLabel();
            bodyLabel.style.fontSize = 16f;
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(bodyLabel);
            panel.Add(scroll);

            captureButton = new Button();
            StyleButton(captureButton);
            captureButton.style.height = 48f;
            captureButton.style.marginTop = 12f;
            panel.Add(captureButton);

            shareButton = new Button(() => actions.shareDebugMotionFrames?.Invoke()) { text = "Share MotionFrames" };
            StyleButton(shareButton);
            shareButton.style.height = 48f;
            shareButton.style.marginTop = 8f;
            panel.Add(shareButton);

            parent.Add(panel);
        }

        public void Render(DebugHudSnapshot snapshot)
        {
            var isAvailable = snapshot?.isAvailable ?? false;
            if (!isAvailable)
            {
                collapsedButton.style.display = DisplayStyle.None;
                panel.style.display = DisplayStyle.None;
                return;
            }

            var isVisible = snapshot.isVisible;
            collapsedButton.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            panel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isVisible)
            {
                return;
            }

            bodyLabel.text = snapshot.lines == null ? string.Empty : string.Join("\n", snapshot.lines);
            captureButton.text = snapshot.captureButtonLabel;
            captureButton.clicked -= StopCapture;
            captureButton.clicked -= StartCapture;
            if (snapshot.isCapturing)
            {
                captureButton.clicked += StopCapture;
            }
            else
            {
                captureButton.clicked += StartCapture;
            }

            shareButton.style.display = snapshot.canShare ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void StartCapture()
        {
            actions.startDebugFrameCapture?.Invoke();
        }

        private void StopCapture()
        {
            actions.stopDebugFrameCapture?.Invoke();
        }

        private static void StyleButton(Button button)
        {
            button.focusable = false;
            button.style.fontSize = 16f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(0.16f, 0.19f, 0.24f, 0.96f);
            button.style.borderTopLeftRadius = 14f;
            button.style.borderTopRightRadius = 14f;
            button.style.borderBottomLeftRadius = 14f;
            button.style.borderBottomRightRadius = 14f;
        }
    }
}
