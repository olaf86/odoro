using System;

namespace Odoro
{
    public interface IMotionSource
    {
        CaptureMode CaptureMode { get; }
        bool IsSupported { get; }

        event Action<MotionFrame> OnFrame;
        event Action<string> OnStatusTextChanged;

        void Activate(MotionSourceActivity activity);
        void Deactivate();
        void Tick(float now);
    }
}
