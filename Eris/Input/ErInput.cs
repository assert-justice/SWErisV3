using ErisMath;
using SDL3;

namespace Eris.Input;

public class ErInput
{
    private bool[] KeyboardState = [];
    private readonly Dictionary<uint, nint> DeviceLookup = [];
    private uint[] Devices = [];
    private SDL.MouseButtonFlags MouseButtonFlags;
    private ErVec2 MousePosition;
    public double GlobalAxisDeadzone{get; set;} = 0.1;
    // private SDL.Event LastEvent;
    public enum ErisInputDeviceKind
    {
        Kbm,
        Gamepad,
    }
    public ErisInputDeviceKind LastEventDevice{get; private set;}
    private static double NormalizeShort(short val)
    {
        return (double)val / 32767;
    }
    public void Poll()
    {
        while (SDL.PollEvent(out var e))
        {
            SDL.EventType eventType = (SDL.EventType)e.Type;
            if(eventType != SDL.EventType.GamepadAxisMotion)
            {
                // Note: Gamepad axis motions are not recorded if they fall below the global deadzone
                // If they are above the deadzone they are added back in below
                // Todo: figure out if I need to handle joysticks too
                // Todo: obviously need to handle controller disconnections
                // LastEvent = e;
            }
            switch (eventType)
            {
                case SDL.EventType.Quit:
                    ErEngine.Quit();
                    break;
                case SDL.EventType.KeyDown:
                case SDL.EventType.KeyUp:
                case SDL.EventType.MouseButtonDown:
                case SDL.EventType.MouseButtonUp:
                case SDL.EventType.MouseMotion:
                    LastEventDevice = ErisInputDeviceKind.Kbm;
                    break;
                case SDL.EventType.GamepadButtonDown:
                case SDL.EventType.GamepadButtonUp:
                    LastEventDevice = ErisInputDeviceKind.Gamepad;
                    break;
                case SDL.EventType.GamepadAxisMotion:
                    double val = NormalizeShort(e.GAxis.Value);
                    if(Math.Abs(val) > GlobalAxisDeadzone)
                    {
                        LastEventDevice = ErisInputDeviceKind.Gamepad;
                        // LastEvent = e;
                    }
                    break;
                default:
                break;
            }
        }
        var kbs = SDL.GetKeyboardState(out int numKeys);
        if(numKeys != KeyboardState.Length)
        {
            KeyboardState = new bool[numKeys];
        }
        for (int idx = 0; idx < numKeys; idx++)
        {
            KeyboardState[idx] = kbs[idx];
        }
        MouseButtonFlags = SDL.GetMouseState(out float mouseX, out float mouseY);
        MousePosition = new(mouseX, mouseY);
        // Note: Gamepad baloney
        uint[] gamepads = SDL.GetGamepads(out _) ?? [];
        HashSet<uint> connected = [..DeviceLookup.Keys];
        foreach (var id in gamepads)
        {
            if (connected.Remove(id)) continue;
            nint gamepadId = SDL.OpenGamepad(id);
            DeviceLookup.Add(id, gamepadId);
        }
        foreach (var id in connected)
        {
            SDL.CloseGamepad(DeviceLookup[id]);
        }
        Devices = gamepads;
    }
    public bool GetKeyDown(SDL.Scancode keyCode)
    {
        return KeyboardState[(int)keyCode];
    }
    public ErVec2 GetMousePosition()
    {
        return MousePosition;
    }
    public bool GetMouseButtonDown(SDL.MouseButtonFlags mouseButton)
    {
        return (int)(MouseButtonFlags & mouseButton) != 0;
    }
    private bool TryGetGamepadId(int deviceId, out nint sdlId)
    {
        sdlId = default;
        if(deviceId < 0 || deviceId >= Devices.Length) return false;
        if(!DeviceLookup.TryGetValue(Devices[deviceId], out sdlId)) return false;
        return true;
    }
    public bool GetGamepadButtonDown(SDL.GamepadButton button, int deviceId = -1)
    {
        if(deviceId < 0) return GetAllGamepadButtonDown(button);
        if(!TryGetGamepadId(deviceId, out nint sdlId)) return false;
        return SDL.GetGamepadButton(sdlId, button);
    }
    public bool GetAllGamepadButtonDown(SDL.GamepadButton button)
    {
        foreach (nint ptr in DeviceLookup.Values)
        {
            if(SDL.GetGamepadButton(ptr, button)) return true;
        }
        return false;
    }
    public double GetGamepadAxis(SDL.GamepadAxis axis, int deviceId = -1)
    {
        if(deviceId < 0) return GetAllGamepadAxis(axis);
        if(!TryGetGamepadId(deviceId, out nint sdlId)) return 0;
        double val = SDL.GetGamepadAxis(sdlId, axis);
        // Note: A short? Really!?
        return val / 32767;
    }
    public double GetAllGamepadAxis(SDL.GamepadAxis axis)
    {
        double val = 0;
        foreach (nint ptr in DeviceLookup.Values)
        {
            short temp = SDL.GetGamepadAxis(ptr, axis);
            val += temp;
        }
        return val / 32767;
    }
}