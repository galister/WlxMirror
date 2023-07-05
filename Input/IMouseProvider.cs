namespace WlxMirror.Input;

public interface IMouseProvider
{
    void MouseMove(int x, int y);
    void SendButton(EvBtn button, bool pressed);
    void Wheel(int delta);
}