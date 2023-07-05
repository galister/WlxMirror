// See https://aka.ms/new-console-template for more information

using WlxMirror.Graphics;
using WlxMirror.Wayland;
using System.CommandLine;
using Nito.AsyncEx.Synchronous;
using Silk.NET.Input;
using WlxMirror.Input;
using WlxMirror.Numerics;
using WlxMirror.Wayland.Capture;
using WlxMirror.Wayland.Capture.Frame;
using WlxMirror.Wayland.Capture.PipeWire;

var outputOpt = new Option<string>("--output", "Output name to mirror, ex: DP-2");
var desktopOpt = new Option<string>("--desktop", "WAYLAND_DISPLAY of desktop");
var mirrorOpt = new Option<string>("--mirror", "WAYLAND_DISPLAY of mirror");
var mouseOpt = new Option<bool>("--mouse", "Enable mouse passthrough");
var colorSwapOpt = new Option<bool>("--colorswap", "Swap red and blue channels (software capture only)");

var rootCommand = new RootCommand();
rootCommand.AddOption(desktopOpt);
rootCommand.AddOption(mirrorOpt);
rootCommand.AddOption(outputOpt);
rootCommand.AddOption(mouseOpt);
rootCommand.AddOption(colorSwapOpt);

rootCommand.SetHandler((ctx) =>
{
    var desktopSocket = ctx.ParseResult.GetValueForOption(desktopOpt) ?? "";
    var mirrorSocket = ctx.ParseResult.GetValueForOption(mirrorOpt) ?? "";
    
    if (desktopSocket == "" || mirrorSocket == "")
        WlSocketFinder.FindSocket(out desktopSocket, out mirrorSocket);
    
    Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", desktopSocket);
    
    Console.WriteLine(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    Console.WriteLine($"Desktop socket: {desktopSocket} Mirror socket: {mirrorSocket}");

    var desktopClient = new DesktopClient(desktopSocket);
    var output = ctx.ParseResult.GetValueForOption(outputOpt);
    var waylandOutput = desktopClient.GetOutput(output);

    ICapture capture;
    if (desktopClient.IsWlr)
        capture = new WlrCapture<DmaBufFrame>(desktopSocket, waylandOutput);
    else
    {
        var nodeId = XdgScreenCastHandler.PromptUserAsync(waylandOutput).WaitAndUnwrapException();

        if (!nodeId.HasValue)
        {
            Console.WriteLine("No capture source available.");
            Environment.Exit(1);
        }
        capture = new PipeWireCapture(waylandOutput, nodeId.Value, ctx.ParseResult.GetValueForOption(colorSwapOpt));
    }
    
    Console.WriteLine($"Capturing output: {waylandOutput.Name}");
    
    var mouseProvider = ctx.ParseResult.GetValueForOption(mouseOpt) ? (IMouseProvider)new UInput() : new DummyMouse();
    
    Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", mirrorSocket);
    Console.WriteLine(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    
    EGL.Initialize();
    
    var glEngine = new GlGraphicsEngine();
    glEngine.Load += (_, rc) =>
    {
        waylandOutput.AllocateTexture(rc);
    };

    var isMousePressed = false;
    var todoActions = new Queue<Action>();
    glEngine.Update += (_, rc) =>
    {
        while (todoActions.TryDequeue(out var action))
            action();
        
        var mouseWasPressed = false;
        foreach (var mouse in rc.Input.Mice)
        {
            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                mouseWasPressed = true;
                if (isMousePressed)
                    break;
                
                var pos = waylandOutput.Transform * (new Vector2(mouse.Position.X, mouse.Position.Y) / new Vector2(rc.Window.Size.X, rc.Window.Size.Y));
                var rectSize = WaylandOutput.OutputRect.Size;
                var mulX = UInput.Extent / rectSize.x;
                var mulY = UInput.Extent / rectSize.y;
                mouseProvider.MouseMove((int)(pos.x * mulX), (int)(pos.y * mulY));
                todoActions.Enqueue(() => mouseProvider.SendButton(EvBtn.Left, true));
                isMousePressed = true;
                break;
            }
        }
        
        if (isMousePressed && !mouseWasPressed)
        {
            mouseProvider.SendButton(EvBtn.Left, false);
            isMousePressed = false;
        }
    };
    
    glEngine.Render += (_, rc) =>
    {
        capture.Render();
        rc.Renderer.Begin(rc.Window.Size);
        rc.Renderer.DrawSprite(waylandOutput.Texture!, 0, 0, rc.Window.Size);
        rc.Renderer.End();
    };

    glEngine.StartEventLoop(waylandOutput.Name, waylandOutput.Size);
});

rootCommand.Invoke(args);
