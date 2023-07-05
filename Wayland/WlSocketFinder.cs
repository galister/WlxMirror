namespace WlxMirror.Wayland;

public class WlSocketFinder
{
    public static void FindSocket(out string desktopSocket, out string mirrorSocket)
    {
        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (runtimeDir == null)
            throw new ApplicationException("XDG_RUNTIME_DIR not set.");
        
        var display = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        if (display == null)
            throw new ApplicationException("WAYLAND_DISPLAY not set.");

        desktopSocket = display;
        
        foreach (var fPath in Directory.GetFiles(runtimeDir))
        {
            var fName = Path.GetFileName(fPath);
            if (fName.StartsWith("wayland-") && !fName.Contains('.') && fName != display)
            {
                mirrorSocket = fName;
                return;
            }
        }

        mirrorSocket = display;
    }
}