using ErisMath;
using SDL3;

namespace Eris.Input.Virtual;

public class ErVAxis2 : ErBaseInput
{
    public readonly List<SDL.Scancode> XPosKeys = [];
    public readonly List<SDL.Scancode> XNegKeys = [];
    public readonly List<SDL.MouseButtonFlags> XPosMouseButtons = [];
    public readonly List<SDL.MouseButtonFlags> XNegMouseButtons = [];
    public readonly List<SDL.GamepadButton> XPosGamepadButtons = [];
    public readonly List<SDL.GamepadButton> XNegGamepadButtons = [];
    public readonly List<SDL.GamepadAxis> XGamepadAxes = [];
    public readonly List<SDL.Scancode> YPosKeys = [];
    public readonly List<SDL.Scancode> YNegKeys = [];
    public readonly List<SDL.MouseButtonFlags> YPosMouseButtons = [];
    public readonly List<SDL.MouseButtonFlags> YNegMouseButtons = [];
    public readonly List<SDL.GamepadButton> YPosGamepadButtons = [];
    public readonly List<SDL.GamepadButton> YNegGamepadButtons = [];
    public readonly List<SDL.GamepadAxis> YGamepadAxes = [];
    public ErVec2 Vector;
    public int GamepadIdx = -2;
    public double Deadzone = 0.2;
    public ErVAxis2(string name) : base(name)
    {
    }
    public override void Poll()
    {
        Vector = Filter(GetState());
    }
    public override void Clear()
    {
        XPosKeys.Clear();
        XNegKeys.Clear();
        XPosMouseButtons.Clear();
        XNegMouseButtons.Clear();
        XPosGamepadButtons.Clear();
        XNegGamepadButtons.Clear();
        XGamepadAxes.Clear();
        YPosKeys.Clear();
        YNegKeys.Clear();
        YPosMouseButtons.Clear();
        YNegMouseButtons.Clear();
        YPosGamepadButtons.Clear();
        YNegGamepadButtons.Clear();
        YGamepadAxes.Clear();
    }
    private ErVec2 Filter(ErVec2 vector)
    {
        double lenSq = vector.GetLengthSquared();
        if(lenSq > 1) return vector.Normalized();
        if(lenSq < Deadzone * Deadzone) return ErVec2.Zero;
        double length = Math.Sqrt(lenSq) - Deadzone;
        length /= 1 - Deadzone;
        return vector.Normalized() * length;
    }
    private ErVec2 GetState()
    {
        double x = 0;
        double y = 0;
        var input = ErEngine.Input;
        foreach (var item in XPosKeys)
        {
            if(input.GetKeyDown(item)) x += 1;
        }
        foreach (var item in XNegKeys)
        {
            if(input.GetKeyDown(item)) x -= 1;
        }
        foreach (var item in XPosMouseButtons)
        {
            if(input.GetMouseButtonDown(item)) x += 1;
        }
        foreach (var item in XNegMouseButtons)
        {
            if(input.GetMouseButtonDown(item)) x -= 1;
        }
        foreach (var item in YPosKeys)
        {
            if(input.GetKeyDown(item)) y += 1;
        }
        foreach (var item in YNegKeys)
        {
            if(input.GetKeyDown(item)) y -= 1;
        }
        foreach (var item in YPosMouseButtons)
        {
            if(input.GetMouseButtonDown(item)) y += 1;
        }
        foreach (var item in YNegMouseButtons)
        {
            if(input.GetMouseButtonDown(item)) y -= 1;
        }
        foreach (var item in XPosGamepadButtons)
        {
            if(input.GetGamepadButtonDown(item, GamepadIdx)) x += 1;
        }
        foreach (var item in XNegGamepadButtons)
        {
            if(input.GetGamepadButtonDown(item, GamepadIdx)) x -= 1;
        }
        foreach (var item in XGamepadAxes)
        {
            x += input.GetGamepadAxis(item, GamepadIdx);
        }
        foreach (var item in YPosGamepadButtons)
        {
            if(input.GetGamepadButtonDown(item, GamepadIdx)) y += 1;
        }
        foreach (var item in YNegGamepadButtons)
        {
            if(input.GetGamepadButtonDown(item, GamepadIdx)) y -= 1;
        }
        foreach (var item in YGamepadAxes)
        {
            y += input.GetGamepadAxis(item, GamepadIdx);
        }
        return new(x, y);
    }
}
