using WlxMirror.Graphics;
using WlxMirror.Wayland.Protocols;
using static Tmds.Linux.LibC;

namespace WlxMirror.Wayland.Capture.Frame;

public class DmaBufFrame : IWaylandFrame
{
    private static ZwlrExportDmabufManagerV1? _dmabufManager;
    
    private static readonly ArrayPool<uint> UintPool = ArrayPool<uint>.Shared;
    private static readonly ArrayPool<int> IntPool = ArrayPool<int>.Shared;
    
    public static void OnGlobal(WlRegistry reg, WlRegistry.GlobalEventArgs e)
    {
        if (e.Interface == WlInterface.ZwlrExportDmabufManagerV1.Name)
            _dmabufManager = reg.Bind<ZwlrExportDmabufManagerV1>(e.Name, e.Interface, e.Version);
    }

    private readonly ZwlrExportDmabufFrameV1 _frame;

    private uint _width;
    private uint _height;
    private uint _format;
    private uint _modLo;
    private uint _modHi;
    private uint _numObjects;

    private int[]? _fds;
    private uint[]? _offsets;
    private uint[]? _pitches;

    private CaptureStatus _status;
    private IntPtr _eglImage;

    public DmaBufFrame(WlOutput output)
    {
        _frame = _dmabufManager!.CaptureOutput(1, output);
        _frame.Frame += OnFrame;
        _frame.Object += OnObject;
        _frame.Cancel += OnCancel;
        _frame.Ready += OnReady;
    }

    public CaptureStatus GetStatus() => _status;

    public void ApplyToTexture(GlTexture texture)
    {
        var pool = ArrayPool<IntPtr>.Shared;
        var attribs = pool.Rent(7 + (int)_numObjects * 10);
        var i = 0;

        attribs[i++] = (IntPtr)EglEnum.Width;
        attribs[i++] = (IntPtr)_width;
        attribs[i++] = (IntPtr)EglEnum.Height;
        attribs[i++] = (IntPtr)_height;
        attribs[i++] = (IntPtr)EglEnum.LinuxDrmFourccExt;
        attribs[i++] = (IntPtr)_format;

        for (var p = 0; p < _numObjects; p++)
        {
            attribs[i++] = (IntPtr)EGL.DmaBufAttribs[p, 0];
            attribs[i++] = (IntPtr)_fds![p];
            attribs[i++] = (IntPtr)EGL.DmaBufAttribs[p, 1];
            attribs[i++] = (IntPtr)_offsets![p];
            attribs[i++] = (IntPtr)EGL.DmaBufAttribs[p, 2];
            attribs[i++] = (IntPtr)_pitches![p];
            attribs[i++] = (IntPtr)EGL.DmaBufAttribs[p, 3];
            attribs[i++] = (IntPtr)_modLo;
            attribs[i++] = (IntPtr)EGL.DmaBufAttribs[p, 4];
            attribs[i++] = (IntPtr)_modHi;
        }

        attribs[i] = (IntPtr)EglEnum.None;

        _eglImage = EGL.CreateImage(EGL.Display, IntPtr.Zero, EglEnum.LinuxDmaBufExt, IntPtr.Zero, attribs);
        var error = EGL.GetError();
        if (error != EglEnum.Success)
            throw new ApplicationException($"{error} on eglCreateImage!");

        pool.Return(attribs);

        texture.LoadEglImage(_eglImage, _width, _height);
    }

    private void OnReady(object? _, ZwlrExportDmabufFrameV1.ReadyEventArgs e)
    {
        _status = CaptureStatus.FrameReady;
    }

    private void OnCancel(object? _, ZwlrExportDmabufFrameV1.CancelEventArgs e)
    {
        _status = e.Reason == ZwlrExportDmabufFrameV1CancelReason.Permanent
            ? CaptureStatus.Fatal
            : CaptureStatus.FrameSkipped;
    }

    private void OnObject(object? _, ZwlrExportDmabufFrameV1.ObjectEventArgs e)
    {
        _fds![e.Index] = e.Fd;
        _offsets![e.Index] = e.Offset;
        _pitches![e.Index] = e.Stride;
    }

    private void OnFrame(object? _, ZwlrExportDmabufFrameV1.FrameEventArgs e)
    {
        _width = e.Width;
        _height = e.Height;
        _format = e.Format;
        _modLo = e.ModLow;
        _modHi = e.ModHigh;
        _numObjects = e.NumObjects;

        _fds = IntPool.Rent((int)e.NumObjects);
        _pitches = UintPool.Rent((int)e.NumObjects);
        _offsets = UintPool.Rent((int)e.NumObjects);
    }

    public void Dispose()
    {
        if (_fds != null)
        {
            for (var i = 0; i < _numObjects; i++)
                if (_fds[i] != 0)
                    close(_fds[i]);

            IntPool.Return(_fds);
        }

        if (_pitches != null)
            UintPool.Return(_pitches);

        if (_offsets != null)
            UintPool.Return(_offsets);

        if (_eglImage != IntPtr.Zero)
            EGL.DestroyImage(EGL.Display, _eglImage);
        _frame.Dispose();
    }
}
