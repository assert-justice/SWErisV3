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
    public bool Pressed => GetPressed();
    public bool JustPressed => GetJustPressed();
    public int GamepadIdx = -2;
    public double Deadzone = ErEngine.Input.GlobalAxisDeadzone;
    private bool State = false;
    private bool LastState = false;
    public ErVButton(string name) : base(name)
    {
    }
    public override void Poll()
    {
        LastState = State;
        State = GetState();
    }
    public override void Clear()
    {
        Keys.Clear();
        MouseButtons.Clear();
        GamepadButtons.Clear();
        GamepadAxesHigh.Clear();
        GamepadAxesLow.Clear();
    }
    private bool GetState()
    {
        bool res = false;
        foreach (var key in Keys)
        {
            if (ErEngine.Input.GetKeyDown(key)) res = true;
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
            if(ErEngine.Input.GetGamepadAxis(axis, GamepadIdx) < -Deadzone) res = true;
        }
        foreach (var axis in GamepadAxesHigh)
        {
            if(ErEngine.Input.GetGamepadAxis(axis, GamepadIdx) > Deadzone) res = true;
        }
        return res;
    }
    private bool GetPressed()
    {
        return State;
    }
    private bool GetJustPressed()
    {
        return State && !LastState;
    }
}
