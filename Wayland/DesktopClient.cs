using WlxMirror.Wayland.Protocols;

namespace WlxMirror.Wayland;

public class DesktopClient : IDisposable
{
    private readonly Dictionary<uint, WaylandOutput> _outputs = new();
    private readonly WlDisplay _display;
    private ZxdgOutputManagerV1? _outputManager;
    private WlSeat? _seat;
    
    public bool IsWlr { get; private set; }
    
    public DesktopClient(string wlDisplay)
    {
        _display = WlDisplay.Connect(wlDisplay);

        var reg = _display.GetRegistry();

        reg.Global += (_, e) =>
        {
            if (e.Interface == WlInterface.WlOutput.Name)
                _ = CreateOutputAsync(reg, e);
            else if (e.Interface == WlInterface.WlSeat.Name)
                _seat = reg.Bind<WlSeat>(e.Name, e.Interface, e.Version);
            else if (e.Interface == WlInterface.ZxdgOutputManagerV1.Name)
                _outputManager = reg.Bind<ZxdgOutputManagerV1>(e.Name, e.Interface, e.Version);
            else if (e.Interface == WlInterface.ZwlrExportDmabufManagerV1.Name)
                IsWlr = true;
        };

        reg.GlobalRemove += (_, e) =>
        {
            if (!_outputs.TryGetValue(e.Name, out var output))
                return;

            _outputs.Remove(e.Name);
            output.Dispose();
        };

        var maxTries = 100;
        while (_outputs.Count == 0 && maxTries-- > 0)
        {
            _display.Roundtrip();
            Thread.Sleep(50);
        }
        Console.WriteLine("Detected {0} outputs.", _outputs.Count);
    }
    
    private async Task CreateOutputAsync(WlRegistry reg, WlRegistry.GlobalEventArgs e)
    {
        var wlOutput = reg.Bind<WlOutput>(e.Name, e.Interface, e.Version);

        while (_outputManager == null)
            await Task.Delay(10);

        var obj = new WaylandOutput(e.Name, wlOutput);

        wlOutput.Geometry += obj.SetGeometry;
        wlOutput.Mode += obj.SetMode;

        using var xdgOutput = _outputManager.GetXdgOutput(wlOutput);
        xdgOutput.Name += obj.SetName;
        xdgOutput.LogicalSize += obj.SetSize;
        xdgOutput.LogicalPosition += obj.SetPosition;
        _display.Roundtrip();

        _outputs.Add(e.Name, obj);
        obj.RecalculateTransform();
    }
    
    public WaylandOutput GetOutput(string? name = null)
    {
        return String.IsNullOrEmpty(name) 
            ? _outputs.Values.First() 
            : _outputs.Values.First(x => x.Name == name);
    }
    
    public void Dispose()
    {
        Thread.Sleep(5);

        foreach (var output in _outputs.Values)
            output.Dispose();

        _seat?.Dispose();
        _outputManager?.Dispose();
        _display.Dispose();
    }
}