using WlxMirror.Graphics;

namespace WlxMirror.Wayland.Capture.Frame;

public interface IWaylandFrame : IDisposable
{
    CaptureStatus GetStatus();
    void ApplyToTexture(GlTexture texture);
}

public enum CaptureStatus
{
    Pending,
    FrameReady,
    FrameSkipped,
    Fatal
}