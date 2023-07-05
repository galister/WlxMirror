using Tmds.Linux;
using WlxMirror.Graphics;
using WlxMirror.Wayland.Protocols;
using static Tmds.Linux.LibC;

namespace WlxMirror.Wayland.Capture.Frame
{
    public class ScreenCopyFrame : IWaylandFrame
    {
        private static WlShm? _shm;
        private static ZwlrScreencopyManagerV1? _screencopyManager;
        
        private readonly bool _invertColors;
        private readonly ZwlrScreencopyFrameV1 _frame;

        public static void OnGlobal(WlRegistry reg, WlRegistry.GlobalEventArgs e)
        {
            if (e.Interface == WlInterface.WlShm.Name)
                _shm = reg.Bind<WlShm>(e.Name, e.Interface, e.Version);
            else if (e.Interface == WlInterface.ZwlrScreencopyManagerV1.Name)
                _screencopyManager = reg.Bind<ZwlrScreencopyManagerV1>(e.Name, e.Interface, e.Version);
        }
        
        private uint _width;
        private uint _height;
        private uint _size;
        private int _fd;
        private WlShmPool? _pool;
        private WlBuffer? _buffer;

        private bool _disposed;

        private CaptureStatus _status;

        public ScreenCopyFrame(WlOutput output)
        {
            _frame = _screencopyManager!.CaptureOutput(1, output);
            _frame.Buffer += OnBuffer;
            _frame.Ready += OnReady;
            _frame.Failed += OnFailed;
            _invertColors = false;
        }

        public CaptureStatus GetStatus() => _status;

        public unsafe void ApplyToTexture(GlTexture texture)
        {
            var ptr = mmap((void*)0, _size, 0x01, 0x01, _fd, 0);
            var fmt = _invertColors
                ? GraphicsFormat.RGBA8
                : GraphicsFormat.BGRA8;

            texture.LoadRawImage(new IntPtr(ptr), fmt, _width, _height);
            munmap(ptr, _size);
        }

        private void OnFailed(object? sender, ZwlrScreencopyFrameV1.FailedEventArgs e)
        {
            _status = CaptureStatus.FrameSkipped;
        }

        private void OnReady(object? sender, ZwlrScreencopyFrameV1.ReadyEventArgs e)
        {
            _status = CaptureStatus.FrameReady;
        }

        private void OnBuffer(object? sender, ZwlrScreencopyFrameV1.BufferEventArgs e)
        {
            _width = e.Width;
            _height = e.Height;
            _size = e.Stride * e.Height;

            _fd = shm_open("/wlxoverlay-screencopy", O_CREAT | O_RDWR, S_IRUSR | S_IWUSR);
            shm_unlink("/wlxoverlay-screencopy");
            ftruncate(_fd, _size);

            _pool = _shm.CreatePool(_fd, (int)_size);
            _buffer = _pool.CreateBuffer(0, (int)e.Width, (int)e.Height, (int)e.Stride, e.Format);
            _frame.Copy(_buffer!);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _buffer?.Destroy();
            _pool?.Destroy();
            if (_fd != 0) close(_fd);
            _frame.Dispose();
            _disposed = true;
        }

        [DllImport("libc")]
        private static extern int shm_open([MarshalAs(UnmanagedType.LPStr)] string name, int oFlags, mode_t mode);

        [DllImport("libc")]
        private static extern int shm_unlink([MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
