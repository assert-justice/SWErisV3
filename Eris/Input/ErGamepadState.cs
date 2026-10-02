using SDL3;

namespace Eris.Input;

internal class ErGamepadState
{
    public readonly nint Handle;
    public readonly uint SdlId;
    private readonly bool[] UsedButtons = new bool[(int)SDL.GamepadButton.Count];
    private readonly bool[] UsedAxes = new bool[(int)SDL.GamepadAxis.Count];
    public ErGamepadState(nint handle, uint sdlId)
    {
        Handle = handle;
        SdlId = sdlId;
    }
    public void Poll()
    {
        Array.Fill(UsedButtons, false);
        Array.Fill(UsedAxes, false);
    }
    public bool HandleGamepadButtonDown(SDL.GamepadButton button)
    {
        if(!SDL.GetGamepadButton(Handle, button)) return false;
        if(UsedButtons[(int)button]) return false;
        UsedButtons[(int)button] = true;
        return true;
    }
    public double HandleGamepadAxis(SDL.GamepadAxis axis)
    {
        double val = SDL.GetGamepadAxis(Handle, axis) / 32767.0;
        if(Math.Abs(val) < ErEngine.Input.GlobalAxisDeadzone) return 0;
        if(UsedAxes[(int)axis]) return 0;
        return val;
    }
}