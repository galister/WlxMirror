# WlxMirror

This is a simple tool that does the following:
- Capture a wayland screen
- Create a window and display the captured image
- Optional: Relay mouse clicks from the window to the captured screen

This project is a spin-off of [WlxOverlay](https://github.com/galister/WlxOverlay).

# Example use cases

### Use between 2 wayland servers

Capture your real desktop and make it visible as an interactive window inside XrDesktop or StardustXR!

```bash
./WlxMirror --desktop wayland-0 --mirror wayland-1 --output DP-2 --mouse
```

### View a virtual or powered-off screen

```bash
./WlxMirror --output DP-2
```

# Build & Run

On any wlroots-based compositor:

```bash 
dotnet run
```

On Gnome, KDE or any other Wayland compositor:

```bash
./prebuild.sh
dotnet run
```

# Works Used
- [Godot Engine](https://github.com/godotengine/godot), MIT License
- [Nito.AsyncEx](https://github.com/StephenCleary/AsyncEx), MIT License
- [OBS Studio](https://github.com/obsproject/obs-studio), GPLv2 License
- [Silk.NET](https://github.com/dotnet/Silk.NET), MIT License
- [Tmds.DBus](https://github.com/tmds/Tmds.DBus), MIT License
- [Tdms.LibC](https://github.com/tmds/Tmds.LibC), MIT License
- [bendahl/uinput](https://github.com/bendahl/uinput), MIT License
- [WaylandSharp](https://github.com/X9VoiD/WaylandSharp), MIT License
