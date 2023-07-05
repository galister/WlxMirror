using System.Reflection;
using WlxMirror.Wayland.Capture.Frame;
using WlxMirror.Wayland.Protocols;

namespace WlxMirror.Wayland.Capture;

public sealed class WlrCapture<T> : IDisposable where T : IWaylandFrame
{
    private readonly WlDisplay Display;
    private readonly WaylandOutput Screen;
    private WlOutput Output = null!;
    
    private T? _frame;
    private T? _lastFrame;
    private readonly TimeSpan RoundTripSleepTime = TimeSpan.FromMilliseconds(1);
    
    private readonly CancellationTokenSource _cancel = new();
    private Task? _worker;

    public WlrCapture(string wlDisplay, WaylandOutput wlOutput)
    {
        Display = WlDisplay.Connect(wlDisplay);
        Screen = wlOutput;

        var reg = Display.GetRegistry();

        reg.Global += (_, e) =>
        {
            if (e.Interface == WlInterface.WlOutput.Name)
            {
                if (e.Name == Screen.IdName)
                    Output = reg.Bind<WlOutput>(e.Name, e.Interface, e.Version);
            }
            else OnGlobal(reg, e);
        };

        reg.GlobalRemove += (_, e) =>
        {
            if (e.Name == Screen.IdName)
                Dispose();
        };

        Display.Roundtrip();
    }

    public void Render()
    {
        var wantNewFrame = true;

        if (_worker is { Status: TaskStatus.RanToCompletion })
        {
            _worker.Dispose();
            switch (_frame!.GetStatus())
            {
                case CaptureStatus.FrameReady:
                    _frame.ApplyToTexture(Screen.Texture!);
                    break;
                case CaptureStatus.FrameSkipped:
                    Console.WriteLine($"{Screen.Name}: Frame was skipped.");
                    break;
                case CaptureStatus.Fatal:
                    Dispose();
                    return;
            }
        }
        else if (_worker != null)
            wantNewFrame = false;

        if (wantNewFrame)
            _worker = Task.Run(RequestNewFrame, _cancel.Token);
    }
    private void RequestNewFrame()
    {
        _lastFrame?.Dispose();
        _lastFrame = _frame;
        _frame = (T)Activator.CreateInstance(typeof(T), Output)!;
        Display.Roundtrip();
        while (_frame.GetStatus() == CaptureStatus.Pending)
        {
            Thread.Sleep(RoundTripSleepTime);
            Display.Roundtrip();
        }
    }
    
    public void Dispose()
    {
        Display.Dispose();
    }
    
    private static void OnGlobal(WlRegistry reg, WlRegistry.GlobalEventArgs e)
    {
        typeof(T).GetMethod("OnGlobal", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object?[] { reg, e });
    }
}