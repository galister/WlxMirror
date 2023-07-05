namespace WlxMirror.Input;

public class DummyMouse : IMouseProvider
{
    public void MouseMove(int x, int y) { }
    public void SendButton(EvBtn button, bool pressed) { }
    public void Wheel(int delta) { }
}