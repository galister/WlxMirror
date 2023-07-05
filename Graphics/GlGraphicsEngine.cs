using System.Reflection;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using WlxMirror.Numerics;

namespace WlxMirror.Graphics;

public sealed class GlGraphicsEngine
{    
    public static readonly string AppDir = Environment.GetEnvironmentVariable("APPDIR") != null
        ? Path.Combine(Environment.GetEnvironmentVariable("APPDIR")!, "usr", "bin")
        : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private GL _gl = null!;
    
    private RenderContext _renderContext = new();

    public static GlShader SpriteShader = null!;
    public static GlShader ColorShader = null!;

    public event EventHandler<RenderContext>? Load;
    public event EventHandler<RenderContext>? Update;
    public event EventHandler<RenderContext>? Render;

    public void StartEventLoop(string name, Vector2Int windowSize)
    {
        var options = WindowOptions.Default;
        // options.IsVisible = false;
        options.Size = new Vector2D<int>(windowSize.X, windowSize.Y);
        options.Title = $"WlxMirror - {name}";
        options.WindowClass = "WlxMirror";
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Compatability, ContextFlags.ForwardCompatible, new APIVersion(4, 5));
        options.VSync = false;
        
        GlfwWindowing.Use();

        _renderContext.Window = Window.Create(options);
        _renderContext.Engine = this;
        _renderContext.Window.Load += OnLoad;
        _renderContext.Window.Update += OnUpdate;
        _renderContext.Window.Render += OnRender;
        _renderContext.Window.Run();
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_renderContext.Window.GLContext);
        _gl.Enable(EnableCap.Texture2D);
        _gl.Enable(EnableCap.Blend);

        _gl.GetError();
        Console.WriteLine("GL Context initialized");

        var vertShader = GetShaderPath("common.vert");
        SpriteShader = new GlShader(_gl, vertShader, GetShaderPath("sprite.frag"));
        ColorShader = new GlShader(_gl, vertShader, GetShaderPath("color.frag"));
        
        _renderContext.Input = _renderContext.Window.CreateInput();
        _renderContext.Renderer = new GlUiRenderer(_gl);
        
        Load?.Invoke(this, _renderContext);
    }

    private string GetShaderPath(string shader)
    {
        return Path.Combine(AppDir, "Shaders", shader);
    }

    private void OnUpdate(double _)
    {
        Update?.Invoke(this, _renderContext);
    }
    
    private void OnRender(double _)
    {
        Render?.Invoke(this, _renderContext);
    }

    public GlTexture TextureFromFile(string path, GraphicsFormat internalFormat = GraphicsFormat.RGBA8)
    {
        var internalFmt = GraphicsFormatAsInternal(internalFormat);
        return new GlTexture(_gl, path, internalFmt);
    }

    public GlTexture EmptyTexture(uint width, uint height, GraphicsFormat internalFormat = GraphicsFormat.RGBA8, bool dynamic = false)
    {
        var internalFmt = GraphicsFormatAsInternal(internalFormat);
        return new GlTexture(_gl, width, height, internalFmt, dynamic);
    }

    public GlTexture TextureFromRaw(uint width, uint height, GraphicsFormat inputFormat, IntPtr data,
        GraphicsFormat internalFormat = GraphicsFormat.RGBA8)
    {
        unsafe
        {
            var internalFmt = GraphicsFormatAsInternal(internalFormat);
            var (pixelFmt, pixelType) = GraphicsFormatAsInput(inputFormat);
            return new GlTexture(_gl, data.ToPointer(), width, height, pixelFmt, pixelType, internalFmt);
        }
    }

    public unsafe GlTexture TextureFromRaw(uint width, uint height, GraphicsFormat inputFormat, Span<byte> data,
        GraphicsFormat internalFormat = GraphicsFormat.RGBA8)
    {
        var internalFmt = GraphicsFormatAsInternal(internalFormat);
        var (pixelFmt, pixelType) = GraphicsFormatAsInput(inputFormat);
        fixed (void* ptr = &data[0])
        {
            return new GlTexture(_gl, ptr, width, height, pixelFmt, pixelType, internalFmt);
        }
    }

    public void Shutdown()
    {
        _renderContext.Window.Close();
    }

    internal static (PixelFormat pf, PixelType pt) GraphicsFormatAsInput(GraphicsFormat format)
    {
        return format switch
        {
            GraphicsFormat.RGBA8 => (PixelFormat.Rgba, PixelType.UnsignedByte),
            GraphicsFormat.BGRA8 => (PixelFormat.Bgra, PixelType.UnsignedByte),
            GraphicsFormat.RGB8 => (PixelFormat.Rgb, PixelType.UnsignedByte),
            GraphicsFormat.RGB_Float => (PixelFormat.Rgb, PixelType.Float),
            GraphicsFormat.BGR8 => (PixelFormat.Bgr, PixelType.UnsignedByte),
            GraphicsFormat.R8 => (PixelFormat.Red, PixelType.UnsignedByte),
            GraphicsFormat.R16 => (PixelFormat.Red, PixelType.UnsignedShort),
            GraphicsFormat.R32 => (PixelFormat.Red, PixelType.UnsignedInt),
            GraphicsFormat.RG8 => (PixelFormat.RG, PixelType.UnsignedByte),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    internal static InternalFormat GraphicsFormatAsInternal(GraphicsFormat format)
    {
        return format switch
        {
            GraphicsFormat.RGBA8 => InternalFormat.Rgba8,
            GraphicsFormat.RGB8 => InternalFormat.Rgb8,
            GraphicsFormat.RG8 => InternalFormat.RG8,
            GraphicsFormat.R8 => InternalFormat.R8,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}
