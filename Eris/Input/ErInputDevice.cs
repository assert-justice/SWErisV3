namespace Eris.Input;

public readonly struct ErInputDevice
{
    public readonly bool UseKeyboard;
    public readonly bool UseGamepad;
    public readonly int GamepadId;
    public ErInputDevice(){}
    private ErInputDevice(bool useKeyboard, bool useGamepad, int gamepadId = -1)
    {
        UseKeyboard = useKeyboard;
        UseGamepad = useGamepad;
        GamepadId = gamepadId;
    }
    public static ErInputDevice All()
    {
        return new(true, true);
    }
    public static ErInputDevice Kbm()
    {
        return new(true, false);
    }
    public static ErInputDevice Gamepad(int gamepadId = -1)
    {
        return new(false, true, gamepadId);
    }
}