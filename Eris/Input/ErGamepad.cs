using SDL3;

namespace Eris.Input;

public class ErGamepad
{
    private static readonly int NumButtons = (int)SDL.GamepadButton.Count;
    private static readonly int NumAxes = (int)SDL.GamepadAxis.Count;
    public readonly nint Handle;
    public readonly uint SdlId;
    public double ButtonDeadzone = 0.2;
    private readonly ErInputBuffer ButtonBuffer = new();
    private readonly ErInputBuffer LowAxisBuffer = new();
    private readonly ErInputBuffer HighAxisBuffer = new();
    public ErGamepad(nint handle, uint sdlId)
    {
        Handle = handle;
        SdlId = sdlId;
    }
    public void Poll()
    {
        ButtonBuffer.Process(Enumerable.Range(0, NumButtons).Where(idx => SDL.GetGamepadButton(Handle, (SDL.GamepadButton)idx)));
        LowAxisBuffer.Process(Enumerable.Range(0, NumAxes).Where(idx => GetAxis(idx) < -ButtonDeadzone));
        HighAxisBuffer.Process(Enumerable.Range(0, NumAxes).Where(idx => GetAxis(idx) > ButtonDeadzone));
    }
    public bool GetGamepadButtonDown(SDL.GamepadButton button)
    {
        return SDL.GetGamepadButton(Handle, button);
    }
    public bool GetGamepadButtonJustDown(SDL.GamepadButton button, double buffer)
    {
        // return SDL.GetGamepadButton(Handle, button);
        return ButtonBuffer.GetJustDown((int)button, buffer);
    }
    public bool GetGamepadAxisLow(SDL.GamepadAxis axis)
    {
        return LowAxisBuffer.GetDown((int)axis);
    }
    public bool GetGamepadAxisJustLow(SDL.GamepadAxis axis, double buffer)
    {
        return LowAxisBuffer.GetJustDown((int)axis, buffer);
    }
    public bool GetGamepadAxisHigh(SDL.GamepadAxis axis)
    {
        return HighAxisBuffer.GetDown((int)axis);
    }
    public bool GetGamepadAxisJustHigh(SDL.GamepadAxis axis, double buffer)
    {
        return HighAxisBuffer.GetJustDown((int)axis, buffer);
    }
    public double GetGamepadAxis(SDL.GamepadAxis axis)
    {
        var val = (double)SDL.GetGamepadAxis(Handle, axis) / short.MaxValue;
        if(Math.Abs(val) < ErEngine.Input.GlobalAxisDeadzone) return 0;
        return val;
    }
    private double GetAxis(int idx)
    {
        return (double)SDL.GetGamepadAxis(Handle, (SDL.GamepadAxis)idx) / short.MaxValue;
    }
}
