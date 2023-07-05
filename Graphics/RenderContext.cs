using Silk.NET.Input;
using Silk.NET.Windowing;

namespace WlxMirror.Graphics;

public class RenderContext
{
    public GlGraphicsEngine Engine;
    public GlUiRenderer Renderer;
    public IInputContext Input;
    public IWindow Window;
}