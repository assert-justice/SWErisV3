using ErisMath;
using Prion.Node;
using SDL3;

namespace Eris.Input;

public class ErVAxis2{
    private readonly SDL.Scancode[] XPosKeys = [];
    private readonly SDL.Scancode[] XNegKeys = [];
    private readonly SDL.MouseButtonFlags[] XPosMouseButtons = [];
    private readonly SDL.MouseButtonFlags[] XNegMouseButtons = [];
    private readonly SDL.GamepadButton[] XPosGamepadButtons = [];
    private readonly SDL.GamepadButton[] XNegGamepadButtons = [];
    private readonly SDL.GamepadAxis[] XGamepadAxes = [];
    private readonly SDL.Scancode[] YPosKeys = [];
    private readonly SDL.Scancode[] YNegKeys = [];
    private readonly SDL.MouseButtonFlags[] YPosMouseButtons = [];
    private readonly SDL.MouseButtonFlags[] YNegMouseButtons = [];
    private readonly SDL.GamepadButton[] YPosGamepadButtons = [];
    private readonly SDL.GamepadButton[] YNegGamepadButtons = [];
    private readonly SDL.GamepadAxis[] YGamepadAxes = [];
    private readonly int GamepadId = -1;
    public readonly double Deadzone = 0.2;
    public ErVec2 Vector{get; private set;}
    public ErVAxis2(){}
    public ErVAxis2(PriNode data)
    {
        if(data.Get("deadzone").TryAs(out double d)) Deadzone = d;
        var x = data.Get("x");
        var y = data.Get("y");
        XPosKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(x.Get("pos_keys"))];
        XNegKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(x.Get("neg_keys"))];
        XPosMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(x.Get("pos_mouse_buttons"))];
        XNegMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(x.Get("neg_mouse_buttons"))];
        YPosKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(y.Get("pos_keys"))];
        YNegKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(y.Get("neg_keys"))];
        YPosMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(y.Get("pos_mouse_buttons"))];
        YNegMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(y.Get("neg_mouse_buttons"))];
        XPosGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(x.Get("pos_gamepad_buttons"))];
        XNegGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(x.Get("neg_gamepad_buttons"))];
        XGamepadAxes = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(x.Get("gamepad_axes"))];
        YPosGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(y.Get("pos_gamepad_buttons"))];
        YNegGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(y.Get("neg_gamepad_buttons"))];
        YGamepadAxes = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(y.Get("gamepad_axes"))];
        // if (device.UseKeyboard)
        // {
        // }
        // if (device.UseGamepad)
        // {
        // }
    }
    private ErVec2 GetState(ErInputDevice device)
    {
        double x = 0;
        double y = 0;
        var input = ErEngine.Input;
        if (device.UseKeyboard)
        {
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
        }
        if (device.UseGamepad)
        {
            foreach (var item in XPosGamepadButtons)
            {
                if(input.GetGamepadButtonDown(item, GamepadId)) x += 1;
            }
            foreach (var item in XNegGamepadButtons)
            {
                if(input.GetGamepadButtonDown(item, GamepadId)) x -= 1;
            }
            foreach (var item in XGamepadAxes)
            {
                x += input.GetGamepadAxis(item, GamepadId);
            }
            foreach (var item in YPosGamepadButtons)
            {
                if(input.GetGamepadButtonDown(item, GamepadId)) y += 1;
            }
            foreach (var item in YNegGamepadButtons)
            {
                if(input.GetGamepadButtonDown(item, GamepadId)) y -= 1;
            }
            foreach (var item in YGamepadAxes)
            {
                y += input.GetGamepadAxis(item, GamepadId);
            }
        }
        return new(x, y);
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
    public ErVec2 Poll(ErInputDevice device)
    {
        Vector = Filter(GetState(device));
        return Vector;
    }
}