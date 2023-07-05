using WlxMirror.Graphics;
using WlxMirror.Numerics;
using WlxMirror.Wayland.Protocols;

namespace WlxMirror.Wayland;

public sealed class WaylandOutput : IDisposable
{
    public static Rect2 OutputRect { get; private set; }
    public string Name { get; set; } = null!;
    public Vector2Int Position { get; set; }
    public Vector2Int Size { get; set; }
    public Transform2D Transform { get; protected set; } = Transform2D.Identity;
    public string Model { get; private set; } = null!;
    public WlOutput? Handle;
    private WlOutputTransform? WlTransform;
    
    public uint IdName;
    
    public GlTexture? Texture;
    
    public Vector2Int LogicalSize { get; protected set; }

    public WaylandOutput(uint idName, WlOutput? handle)
    {
        IdName = idName;
        Handle = handle;
    }

    internal void SetPosition(object? _, ZxdgOutputV1.LogicalPositionEventArgs e)
    {
        Position = new Vector2Int(e.X, e.Y);
    }

    internal void SetName(object? _, ZxdgOutputV1.NameEventArgs e)
    {
        Name = e.Name;
    }
    
    internal void SetSize(object? _, ZxdgOutputV1.LogicalSizeEventArgs e)
    {
        LogicalSize = new Vector2Int(e.Width, e.Height);
    }
    
    internal void SetGeometry(object? _, WlOutput.GeometryEventArgs e)
    {
        Model = e.Model;
        WlTransform = e.Transform;
    }
    
    internal void SetMode(object? _, WlOutput.ModeEventArgs e)
    {
        Size = new Vector2Int(e.Width, e.Height);
    }

    public void RecalculateTransform()
    {
        var size = LogicalSize;
        switch (WlTransform)
        {
            case WlOutputTransform._90:
            case WlOutputTransform.Flipped90:
                Transform = new Transform2D(0, size.Y, -size.X, 0, Position.X+size.X, Position.Y);
                break;
            case WlOutputTransform._180:
            case WlOutputTransform.Flipped180:
                Transform = new Transform2D(-size.X, 0, 0, -size.Y, Position.X+size.X, Position.Y+size.Y);
                break;
            case WlOutputTransform._270:
            case WlOutputTransform.Flipped270:
                Transform = new Transform2D(0, -size.Y, size.X, 0, Position.X, Position.Y+size.Y);
                break;
            default:
                Transform = new Transform2D(size.X, 0, 0, size.Y, Position.X, Position.Y);
                break;
        }
        MergeOutputRect();
    }
    private void MergeOutputRect()
    {
        var origin = Transform * Vector2.Zero;
        var size = Transform * Vector2.One - origin;
        if (size.x < 0)
        {
            origin.x += size.x;
            size.x = -size.x;
        }

        if (size.y < 0)
        {
            origin.y += size.y;
            size.y = -size.y;
        }

        var rect = new Rect2(origin, size);
        OutputRect = OutputRect.Merge(rect);
    }

    internal void CopyTo(WaylandOutput other)
    {
        other.Handle = Handle;
        other.IdName = IdName;
        other.LogicalSize = LogicalSize;
        other.Model = Model;
        other.Name = Name;
        other.Position = Position;
        other.Size = Size;
        other.Transform = Transform;
    }

    public void AllocateTexture(RenderContext ctx)
    {
        Texture = ctx.Engine.EmptyTexture((uint)Size.X, (uint)Size.Y, dynamic: true);
    }

    public void Dispose()
    {
        Handle?.Dispose();
    }
}
