using SDL3;

namespace Eris.Input;

public class ErGamepad
{
    public readonly nint Handle;
    public readonly uint SdlId;
    private readonly bool[] UsedButtons = new bool[(int)SDL.GamepadButton.Count];
    private readonly bool[] UsedAxes = new bool[(int)SDL.GamepadAxis.Count];
    public ErGamepad(nint handle, uint sdlId)
    {
        Handle = handle;
        SdlId = sdlId;
    }
    public void Poll()
    {
        Array.Fill(UsedButtons, false);
        Array.Fill(UsedAxes, false);
    }
    public bool GetGamepadButtonDown(SDL.GamepadButton button)
    {
        return SDL.GetGamepadButton(Handle, button);
    }
    public bool HandleGamepadButtonDown(SDL.GamepadButton button)
    {
        if(!GetGamepadButtonDown(button)) return false;
        if(UsedButtons[(int)button]) return false;
        UsedButtons[(int)button] = true;
        return true;
    }
    public double GetGamepadAxis(SDL.GamepadAxis axis)
    {
        var val = (double)SDL.GetGamepadAxis(Handle, axis) / short.MaxValue;
        if(Math.Abs(val) < ErEngine.Input.GlobalAxisDeadzone) return 0;
        return val;
    }
    public double HandleGamepadAxis(SDL.GamepadAxis axis)
    {
        double val = GetGamepadAxis(axis);
        if(val == 0) return 0;
        if(UsedAxes[(int)axis]) return 0;
        return val;
    }
}