using System.Text;
using ErisMath;
using SDL3;

namespace Eris.Input;

public class ErInput
{
    private bool[] KeyboardState = [];
    // private readonly Dictionary<uint, nint> DeviceLookup = [];
    // private uint[] Devices = [];
    private readonly Dictionary<uint, ErGamepadState> GamepadStates = [];
    private SDL.MouseButtonFlags MouseButtonFlags;
    private ErVec2 MousePosition;
    public double GlobalAxisDeadzone{get; set;} = 0.1;
    public enum DeviceKind
    {
        Kbm,
        Gamepad,
    }
    public DeviceKind LastEventDevice{get; private set;}
    private static double NormalizeShort(short val)
    {
        return (double)val / 32767;
    }
    public void Poll()
    {
        // Todo: obviously need to handle controller disconnections
        while (SDL.PollEvent(out var e))
        {
            SDL.EventType eventType = (SDL.EventType)e.Type;
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
                    LastEventDevice = DeviceKind.Kbm;
                    break;
                case SDL.EventType.GamepadButtonDown:
                case SDL.EventType.GamepadButtonUp:
                    LastEventDevice = DeviceKind.Gamepad;
                    break;
                case SDL.EventType.GamepadAxisMotion:
                    double val = NormalizeShort(e.GAxis.Value);
                    // Note: Gamepad axis motions are not recorded if they fall below the global deadzone
                    // If they are above the deadzone they are added back in below
                    if(Math.Abs(val) > GlobalAxisDeadzone)
                    {
                        LastEventDevice = DeviceKind.Gamepad;
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
        if(GamepadsOk(gamepads)) return;
        // HashSet<uint> connected = [];
        // foreach (var item in collection)
        // {
            
        // }
        // foreach (var id in gamepads)
        // {
        //     if (connected.Remove(id)) continue;
        //     nint gamepadId = SDL.OpenGamepad(id);
        //     DeviceLookup.Add(id, gamepadId);
        // }
        // foreach (var id in connected)
        // {
        //     SDL.CloseGamepad(DeviceLookup[id]);
        // }
        // Devices = gamepads;
    }
    private bool GamepadsOk(uint[] gamepadIds)
    {
        // if(GamepadStates.Count != gamepadIds.Length) return false;
        // for (int idx = 0; idx < gamepadIds.Length; idx++)
        // {
        //     if(GamepadStates[idx].SdlId != gamepadIds[idx]) return false;
        // }
        return true;
    }
    public bool HandleKeyDown(SDL.Scancode keyCode)
    {
        bool res = KeyboardState[(int)keyCode];
        if(res) KeyboardState[(int)keyCode] = false;
        return res;
    }
    public bool GetKeyDown(SDL.Scancode keyCode)
    {
        bool res = KeyboardState[(int)keyCode];
        // if(res) KeyboardState[(int)keyCode] = false;
        return res;
    }
    public ErVec2 GetMousePosition()
    {
        return MousePosition;
    }
    public bool HandleMouseButtonDown(SDL.MouseButtonFlags mouseButton)
    {
        bool res = (int)(MouseButtonFlags & mouseButton) != 0;
        if(res)
        {
            MouseButtonFlags &= ~mouseButton;
        }
        return res;
    }
    public bool GetMouseButtonDown(SDL.MouseButtonFlags mouseButton)
    {
        bool res = (int)(MouseButtonFlags & mouseButton) != 0;
        // if(res)
        // {
        //     MouseButtonFlags &= ~mouseButton;
        // }
        return res;
    }
    // private IEnumerable<ErGamepadState> GetGamepadStates(uint gamepadId)
    // {
    //     if(gamepadId < 0)
    //     {
    //         foreach (var item in GamepadStates.Values)
    //         {
    //             yield return item;
    //         }
    //     }
    //     else if(GamepadStates.TryGetValue(gamepadId, out var state))
    //     {
            
    //     }
    //     else if(gamepadId >= GamepadStates.Count)
    //     {
    //         ErEngine.LogWarning("bad gamepad index ", gamepadId);
    //     }
    //     else
    //     {
    //         yield return GamepadStates[gamepadId];
    //     }
    // }
    public bool HandleGamepadButtonDown(SDL.GamepadButton button, int gamepadIdx)
    {
        return false;
        // bool res = false;
        // foreach (var item in GetGamepadStates(gamepadIdx))
        // {
        //     if(item.GetGamepadButtonDown(button)) res = true;
        // }
        // return res;
    }
    public bool GetGamepadButtonDown(SDL.GamepadButton button, int gamepadIdx)
    {
        return false;
    }
    public double HandleGamepadAxis(SDL.GamepadAxis axis, int gamepadIdx)
    {
        return 0;
        // double res = 0;
        // foreach (var item in GetGamepadStates(gamepadIdx))
        // {
        //     res += item.GetGamepadAxis(axis);
        // }
        // return Math.Clamp(res, -1, 1);
    }
    public double GetGamepadAxis(SDL.GamepadAxis axis, int gamepadIdx)
    {
        return 0;
    }
    // private bool TryGetGamepadId(int deviceId, out nint sdlId)
    // {
    //     sdlId = default;
    //     if(deviceId < 0 || deviceId >= Devices.Length) return false;
    //     if(!DeviceLookup.TryGetValue(Devices[deviceId], out sdlId)) return false;
    //     return true;
    // }
    // public bool GetGamepadButtonDown(SDL.GamepadButton button, int deviceId = -1)
    // {
    //     if(deviceId < 0) return GetAllGamepadButtonDown(button);
    //     if(!TryGetGamepadId(deviceId, out nint sdlId)) return false;
    //     return SDL.GetGamepadButton(sdlId, button);
    // }
    // public bool GetAllGamepadButtonDown(SDL.GamepadButton button)
    // {
    //     foreach (nint ptr in DeviceLookup.Values)
    //     {
    //         if(SDL.GetGamepadButton(ptr, button)) return true;
    //     }
    //     return false;
    // }
    // public double GetGamepadAxis(SDL.GamepadAxis axis, int deviceId = -1)
    // {
    //     if(deviceId < 0) return GetAllGamepadAxis(axis);
    //     if(!TryGetGamepadId(deviceId, out nint sdlId)) return 0;
    //     double val = SDL.GetGamepadAxis(sdlId, axis);
    //     // Note: A short? Really!?
    //     return val / 32767;
    // }
    // public double GetAllGamepadAxis(SDL.GamepadAxis axis)
    // {
    //     double val = 0;
    //     foreach (nint ptr in DeviceLookup.Values)
    //     {
    //         short temp = SDL.GetGamepadAxis(ptr, axis);
    //         val += temp;
    //     }
    //     return val / 32767;
    // }
}
