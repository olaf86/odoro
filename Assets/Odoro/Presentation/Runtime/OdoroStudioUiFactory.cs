using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed class StepperBinding
    {
        public Label titleLabel;
        public Label valueLabel;
    }

    public sealed class ButtonBinding
    {
        public Button button;
        public Label label;
    }

    public static class OdoroStudioUiFactory
    {
        private static Font uiFont;

        public static VisualElement CreateScreen(string name)
        {
            var screen = new VisualElement { name = name };
            screen.AddToClassList("odoro-screen");
            screen.AddToClassList(name);
            screen.style.flexGrow = 1f;
            screen.style.flexDirection = FlexDirection.Column;
            return screen;
        }

        public static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
        }

        public static VisualElement CreateSpacer(float height)
        {
            var spacer = new VisualElement();
            spacer.style.height = height;
            return spacer;
        }

        public static VisualElement CreatePanel(Color color, float radius)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = color;
            panel.style.borderTopLeftRadius = radius;
            panel.style.borderTopRightRadius = radius;
            panel.style.borderBottomLeftRadius = radius;
            panel.style.borderBottomRightRadius = radius;
            panel.style.paddingLeft = 18f;
            panel.style.paddingRight = 18f;
            panel.style.paddingTop = 18f;
            panel.style.paddingBottom = 18f;
            return panel;
        }

        public static VisualElement CreateCard()
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 0.95f);
            card.style.borderTopLeftRadius = 18f;
            card.style.borderTopRightRadius = 18f;
            card.style.borderBottomLeftRadius = 18f;
            card.style.borderBottomRightRadius = 18f;
            card.style.paddingLeft = 14f;
            card.style.paddingRight = 14f;
            card.style.paddingTop = 12f;
            card.style.paddingBottom = 12f;
            return card;
        }

        public static Label CreateTitleLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 21f, FontStyle.Bold, Color.white);
            return label;
        }

        public static Label CreateSectionLabel(string text)
        {
            var label = new Label(text);
            ApplyTextStyle(label, 18f, FontStyle.Bold, Color.white);
            return label;
        }

        public static Label CreateSubtitleLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 12f, FontStyle.Normal, new Color(0.74f, 0.80f, 0.86f));
            return label;
        }

        public static Label CreateBodyLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 13f, FontStyle.Normal, new Color(0.86f, 0.89f, 0.92f));
            return label;
        }

        public static Label CreateCaptionLabel(string text)
        {
            var label = new Label(text);
            ApplyTextStyle(label, 11f, FontStyle.Normal, new Color(0.72f, 0.78f, 0.84f));
            return label;
        }

        public static Label CreateValueLabel(string text = "")
        {
            var label = new Label(text);
            ApplyTextStyle(label, 15f, FontStyle.Bold, Color.white);
            return label;
        }

        public static Label CreatePillLabel()
        {
            var label = new Label();
            ApplyTextStyle(label, 11f, FontStyle.Bold, Color.white);
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.30f);
            label.style.paddingLeft = 12f;
            label.style.paddingRight = 12f;
            label.style.paddingTop = 7f;
            label.style.paddingBottom = 7f;
            label.style.borderTopLeftRadius = 999f;
            label.style.borderTopRightRadius = 999f;
            label.style.borderBottomLeftRadius = 999f;
            label.style.borderBottomRightRadius = 999f;
            label.style.alignSelf = Align.FlexStart;
            return label;
        }

        public static StepperBinding AddStepper(VisualElement parent, string title, Action decrease, Action increase)
        {
            var card = CreateCard();
            card.style.marginTop = 10f;
            var titleLabel = CreateCaptionLabel(title);
            card.Add(titleLabel);

            var row = CreateRow();
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 8f;

            var decreaseButton = CreateSecondaryButton("-", decrease, 34f);
            decreaseButton.button.style.width = 44f;
            decreaseButton.button.style.marginRight = 8f;
            row.Add(decreaseButton.button);

            var valueLabel = CreateValueLabel("--");
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.flexGrow = 1f;
            row.Add(valueLabel);

            var increaseButton = CreateSecondaryButton("+", increase, 34f);
            increaseButton.button.style.width = 44f;
            increaseButton.button.style.marginLeft = 8f;
            row.Add(increaseButton.button);
            card.Add(row);
            parent.Add(card);

            return new StepperBinding
            {
                titleLabel = titleLabel,
                valueLabel = valueLabel,
            };
        }

        public static ButtonBinding CreatePrimaryButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.18f, 0.52f, 0.87f), Color.white);
        }

        public static ButtonBinding CreateSecondaryButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.16f, 0.19f, 0.24f), Color.white);
        }

        public static ButtonBinding CreateDangerButton(string text, Action action, float height = 40f)
        {
            return CreateButton(text, action, height, new Color(0.86f, 0.19f, 0.22f), Color.white);
        }

        private static ButtonBinding CreateButton(string text, Action action, float height, Color background, Color foreground)
        {
            var button = new Button(() => action?.Invoke());
            button.focusable = false;
            button.style.height = height;
            button.style.borderTopLeftRadius = 18f;
            button.style.borderTopRightRadius = 18f;
            button.style.borderBottomLeftRadius = 18f;
            button.style.borderBottomRightRadius = 18f;
            button.style.backgroundColor = background;
            button.style.paddingLeft = 16f;
            button.style.paddingRight = 16f;
            button.style.paddingTop = 10f;
            button.style.paddingBottom = 10f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;

            var label = new Label(text);
            ApplyTextStyle(label, 14f, FontStyle.Bold, foreground);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore;
            button.Add(label);

            return new ButtonBinding
            {
                button = button,
                label = label,
            };
        }

        private static void ApplyTextStyle(TextElement element, float fontSize, FontStyle fontStyle, Color color)
        {
            element.style.unityFont = uiFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            element.style.fontSize = fontSize;
            element.style.unityFontStyleAndWeight = fontStyle;
            element.style.color = color;
            element.style.whiteSpace = WhiteSpace.Normal;
        }
    }
}
