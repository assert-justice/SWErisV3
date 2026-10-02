using Eris.Input.Virtual;

namespace Eris.Input;

public abstract class ErInputDevice
{
    public readonly string Name = string.Empty;
    protected readonly Dictionary<string, ErVButton> Buttons = [];
    protected readonly Dictionary<string, ErVAxis> Axes = [];
    protected readonly Dictionary<string, ErVAxis2> Axes2 = [];
    public virtual void Poll()
    {
        foreach (var item in Buttons.Values)
        {
            item.Poll();
        }
        foreach (var item in Axes.Values)
        {
            item.Poll();
        }
        foreach (var item in Axes2.Values)
        {
            item.Poll();
        }
    }
}

// public readonly struct ErInputDevice
// {
//     public readonly bool UseKeyboard;
//     public readonly bool UseGamepad;
//     public readonly int GamepadId;
//     public ErInputDevice(){}
//     private ErInputDevice(bool useKeyboard, bool useGamepad, int gamepadId = -1)
//     {
//         UseKeyboard = useKeyboard;
//         UseGamepad = useGamepad;
//         GamepadId = gamepadId;
//     }
//     public static ErInputDevice All()
//     {
//         return new(true, true);
//     }
//     public static ErInputDevice Kbm()
//     {
//         return new(true, false);
//     }
//     public static ErInputDevice Gamepad(int gamepadId = -1)
//     {
//         return new(false, true, gamepadId);
//     }
// }