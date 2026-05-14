namespace Odoro
{
    public sealed class StudioPlaybackCapturedClipPreparer : ICapturedClipPreparer
    {
        private readonly CaptureMode captureMode;

        public StudioPlaybackCapturedClipPreparer(CaptureMode captureMode)
        {
            this.captureMode = captureMode;
        }

        public MotionClip PrepareCapturedClip(MotionClip clip)
        {
            return MotionPlaybackClipPreparer.Prepare(clip, captureMode);
        }
    }
}
