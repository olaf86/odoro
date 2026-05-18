using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public sealed class OdoroScreenTransitionController
    {
        private const float TransitionDurationSeconds = 0.22f;
        private const float SlideDistancePixels = 18f;
        private const float VeilPeakOpacity = 0.22f;

        private readonly Dictionary<StudioScreen, VisualElement> screens;
        private readonly VisualElement veil;

        private StudioScreen activeScreen;
        private bool hasActiveScreen;
        private int transitionVersion;
        private IVisualElementScheduledItem activeTransition;

        public OdoroScreenTransitionController(
            VisualElement parent,
            Dictionary<StudioScreen, VisualElement> screens
        )
        {
            this.screens = screens;

            veil = new VisualElement { name = "odoro-screen-transition-veil" };
            veil.pickingMode = PickingMode.Ignore;
            veil.style.position = Position.Absolute;
            veil.style.left = 0f;
            veil.style.right = 0f;
            veil.style.top = 0f;
            veil.style.bottom = 0f;
            veil.style.backgroundColor = Color.black;
            veil.style.opacity = 0f;
            veil.style.display = DisplayStyle.None;
            parent.Add(veil);

            foreach (var screen in screens.Values)
            {
                HideImmediately(screen);
            }
        }

        public void Show(StudioScreen nextScreen)
        {
            if (!screens.TryGetValue(nextScreen, out var incoming))
            {
                return;
            }

            if (!hasActiveScreen)
            {
                ShowImmediately(nextScreen, incoming);
                return;
            }

            if (activeScreen == nextScreen)
            {
                incoming.style.display = DisplayStyle.Flex;
                return;
            }

            activeTransition?.Pause();
            var outgoing = screens[activeScreen];
            StartTransition(activeScreen, outgoing, nextScreen, incoming);
        }

        private void ShowImmediately(StudioScreen nextScreen, VisualElement incoming)
        {
            foreach (var screen in screens.Values)
            {
                HideImmediately(screen);
            }

            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 1f;
            incoming.style.translate = TranslatePixels(0f);
            veil.style.display = DisplayStyle.None;
            veil.style.opacity = 0f;
            activeScreen = nextScreen;
            hasActiveScreen = true;
        }

        private void StartTransition(
            StudioScreen outgoingScreen,
            VisualElement outgoing,
            StudioScreen incomingScreen,
            VisualElement incoming
        )
        {
            var version = ++transitionVersion;
            var startedAt = Time.unscaledTime;
            var slideDirection = SlideDirection(outgoingScreen, incomingScreen);

            foreach (var screen in screens.Values)
            {
                if (screen != outgoing && screen != incoming)
                {
                    HideImmediately(screen);
                }
            }

            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 0f;
            incoming.style.translate = TranslatePixels(SlideDistancePixels * slideDirection);
            outgoing.style.display = DisplayStyle.Flex;
            outgoing.style.opacity = 1f;
            outgoing.style.translate = TranslatePixels(0f);
            veil.style.display = DisplayStyle.Flex;
            veil.style.opacity = 0f;

            activeScreen = incomingScreen;
            IVisualElementScheduledItem transitionItem = null;
            transitionItem = incoming.schedule.Execute(() =>
            {
                if (version != transitionVersion)
                {
                    transitionItem?.Pause();
                    return;
                }

                var elapsed = Time.unscaledTime - startedAt;
                var progress = Mathf.Clamp01(elapsed / TransitionDurationSeconds);
                var eased = EaseOutCubic(progress);
                var outgoingOpacity = 1f - eased;
                var incomingOpacity = eased;
                var veilOpacity = Mathf.Sin(progress * Mathf.PI) * VeilPeakOpacity;

                incoming.style.opacity = incomingOpacity;
                incoming.style.translate = TranslatePixels(SlideDistancePixels * (1f - eased) * slideDirection);
                outgoing.style.opacity = outgoingOpacity;
                outgoing.style.translate = TranslatePixels(-SlideDistancePixels * 0.45f * eased * slideDirection);
                veil.style.opacity = veilOpacity;

                if (progress >= 1f)
                {
                    CompleteTransition(outgoing, incoming);
                    transitionItem?.Pause();
                }
            }).Every(16);
            activeTransition = transitionItem;
        }

        private void CompleteTransition(VisualElement outgoing, VisualElement incoming)
        {
            outgoing.style.display = DisplayStyle.None;
            outgoing.style.opacity = 0f;
            outgoing.style.translate = TranslatePixels(0f);
            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 1f;
            incoming.style.translate = TranslatePixels(0f);
            veil.style.opacity = 0f;
            veil.style.display = DisplayStyle.None;
        }

        private static void HideImmediately(VisualElement screen)
        {
            screen.style.display = DisplayStyle.None;
            screen.style.opacity = 0f;
            screen.style.translate = TranslatePixels(0f);
        }

        private static int SlideDirection(StudioScreen from, StudioScreen to)
        {
            return ScreenOrder(to) >= ScreenOrder(from) ? 1 : -1;
        }

        private static int ScreenOrder(StudioScreen screen)
        {
            return screen switch
            {
                StudioScreen.Capture => 0,
                StudioScreen.RecordingSettings => 1,
                StudioScreen.ClipsLibrary => 2,
                StudioScreen.Stage => 3,
                _ => 0,
            };
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static Translate TranslatePixels(float y)
        {
            return new Translate(
                new Length(0f, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel),
                0f
            );
        }
    }
}
