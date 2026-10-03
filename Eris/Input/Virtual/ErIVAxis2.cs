using ErisMath;
using Prion.Node;
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
    public ErVec2 Vector;// => GetVector();
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

// public class ErVAxis2{
//     private readonly int GamepadId = -1;
//     public readonly double Deadzone = 0.2;
//     public ErVec2 Vector{get; private set;}
//     public ErVAxis2(){}
//     public ErVAxis2(PriNode data)
//     {
//         if(data.Get("deadzone").TryAs(out double d)) Deadzone = d;
//         var x = data.Get("x");
//         var y = data.Get("y");
//         XPosKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(x.Get("pos_keys"))];
//         XNegKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(x.Get("neg_keys"))];
//         XPosMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(x.Get("pos_mouse_buttons"))];
//         XNegMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(x.Get("neg_mouse_buttons"))];
//         YPosKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(y.Get("pos_keys"))];
//         YNegKeys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(y.Get("neg_keys"))];
//         YPosMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(y.Get("pos_mouse_buttons"))];
//         YNegMouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(y.Get("neg_mouse_buttons"))];
//         XPosGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(x.Get("pos_gamepad_buttons"))];
//         XNegGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(x.Get("neg_gamepad_buttons"))];
//         XGamepadAxes = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(x.Get("gamepad_axes"))];
//         YPosGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(y.Get("pos_gamepad_buttons"))];
//         YNegGamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(y.Get("neg_gamepad_buttons"))];
//         YGamepadAxes = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(y.Get("gamepad_axes"))];
//         // if (device.UseKeyboard)
//         // {
//         // }
//         // if (device.UseGamepad)
//         // {
//         // }
//     }
//     private ErVec2 Filter(ErVec2 vector)
//     {
//         double lenSq = vector.GetLengthSquared();
//         if(lenSq > 1) return vector.Normalized();
//         if(lenSq < Deadzone * Deadzone) return ErVec2.Zero;
//         double length = Math.Sqrt(lenSq) - Deadzone;
//         length /= 1 - Deadzone;
//         return vector.Normalized() * length;
//     }
//     public ErVec2 Poll(ErInputDevice device)
//     {
//         Vector = Filter(GetState(device));
//         return Vector;
//     }
// }