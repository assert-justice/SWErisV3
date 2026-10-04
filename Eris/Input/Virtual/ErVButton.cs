using SDL3;

namespace Eris.Input.Virtual;

public class ErVButton : ErBaseInput
{
    public readonly List<SDL.Scancode> Keys = [];
    public readonly List<SDL.MouseButtonFlags> MouseButtons = [];
    public readonly List<SDL.GamepadButton> GamepadButtons = [];
    public readonly List<SDL.GamepadAxis> GamepadAxesLow = [];
    public readonly List<SDL.GamepadAxis> GamepadAxesHigh = [];
    public double Buffer = 0;
    public bool Down => GetDown();
    public bool JustDown => GetJustDown();
    public int GamepadIdx = -2;
    public double Deadzone = ErEngine.Input.GlobalAxisDeadzone;
    public ErVButton(string name) : base(name)
    {
    }
    public override void Poll(){}
    public override void Clear()
    {
        Keys.Clear();
        MouseButtons.Clear();
        GamepadButtons.Clear();
        GamepadAxesHigh.Clear();
        GamepadAxesLow.Clear();
    }
    private bool GetDown()
    {
        bool res = false;
        foreach (var key in Keys)
        {
            if(ErEngine.Input.GetKeyDown(key)) res = true;
        }
        foreach (var mb in MouseButtons)
        {
            if(ErEngine.Input.GetMouseButtonDown(mb)) res = true;
        }
        foreach (var button in GamepadButtons)
        {
            if(ErEngine.Input.GetGamepadButtonDown(button, GamepadIdx)) res = true;
        }
        foreach (var axis in GamepadAxesLow)
        {
            if(ErEngine.Input.GetGamepadAxisLow(axis, GamepadIdx)) res = true;
        }
        foreach (var axis in GamepadAxesHigh)
        {
            if(ErEngine.Input.GetGamepadAxisHigh(axis, GamepadIdx)) res = true;
        }
        return res;
    }
    private bool GetJustDown()
    {
        bool res = false;
        foreach (var key in Keys)
        {
            if(ErEngine.Input.GetKeyJustDown(key, Buffer)) res = true;
        }
        foreach (var mb in MouseButtons)
        {
            if(ErEngine.Input.GetMouseButtonJustDown(mb, Buffer)) res = true;
        }
        foreach (var button in GamepadButtons)
        {
            if(ErEngine.Input.GetGamepadButtonJustDown(button, GamepadIdx, Buffer)) res = true;
        }
        foreach (var axis in GamepadAxesLow)
        {
            if(ErEngine.Input.GetGamepadAxisJustLow(axis, GamepadIdx, Buffer)) res = true;
        }
        foreach (var axis in GamepadAxesHigh)
        {
            if(ErEngine.Input.GetGamepadAxisJustHigh(axis, GamepadIdx, Buffer)) res = true;
        }
        return res;
    }
}
