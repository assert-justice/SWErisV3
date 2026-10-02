using System.Text;
using ErisMath;
using SDL3;

namespace Eris.Input;

public class ErInput
{
    private bool[] KeyboardState = [];
    private readonly Dictionary<uint, ErGamepad> GamepadLookup = [];
    private readonly List<ErGamepad?> Gamepads = [];
    private SDL.MouseButtonFlags MouseButtonFlags;
    private ErVec2 MousePosition;
    public double GlobalAxisDeadzone{get; set;} = 0.1;
    public Action<int> OnGamepadConnect = idx => ErEngine.Log("gamepad ", idx, " connected");
    public Action<int> OnGamepadDisconnect = idx => ErEngine.Log("gamepad ", idx, " disconnected");
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
        HandleGamepadConnections();
        foreach (var gamepad in GamepadLookup.Values)
        {
            gamepad.Poll();
        }
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
        return res;
    }
    public bool TryGetGamepad(int gamepadIdx, out ErGamepad gamepad)
    {
        gamepad = default!;
        if(gamepadIdx < 0 || gamepadIdx >= Gamepads.Count) return false;
        var gp = Gamepads[gamepadIdx];
        if(gp is null) return false;
        gamepad = gp;
        return true;
    }
    public IEnumerable<(int gamepadIdx, ErGamepad gamepad)> GetAllGamepads()
    {
        for (int idx = 0; idx < Gamepads.Count; idx++)
        {
            var gp = Gamepads[idx];
            if(gp is null) continue;
            yield return (idx, gp);
        }
    }
    public bool GetGamepadButtonDown(SDL.GamepadButton button, int gamepadIdx)
    {
        foreach (var gp in GetGamepads(gamepadIdx))
        {
            if(gp.GetGamepadButtonDown(button)) return true;
        }
        return false;
    }
    public bool HandleGamepadButtonDown(SDL.GamepadButton button, int gamepadIdx)
    {
        bool res = false;
        foreach (var item in GetGamepads(gamepadIdx))
        {
            if(item.GetGamepadButtonDown(button)) res = true;
        }
        return res;
    }
    public double GetGamepadAxis(SDL.GamepadAxis axis, int gamepadIdx)
    {
        double res = 0;
        foreach (var item in GetGamepads(gamepadIdx))
        {
            res += item.GetGamepadAxis(axis);
        }
        return Math.Clamp(res, -1, 1);
    }
    public double HandleGamepadAxis(SDL.GamepadAxis axis, int gamepadIdx)
    {
        double res = 0;
        foreach (var item in GetGamepads(gamepadIdx))
        {
            res += item.GetGamepadAxis(axis);
        }
        return Math.Clamp(res, -1, 1);
    }
    private IEnumerable<ErGamepad> GetGamepads(int gamepadIdx)
    {
        if(gamepadIdx == -1)
        {
            foreach (var gp in Gamepads)
            {
                if(gp is not null) yield return gp;
            }
        }
        else if(TryGetGamepad(gamepadIdx, out var gamepad)) yield return gamepad;
    }
    private void HandleGamepadConnections()
    {
        uint[] gamepads = SDL.GetGamepads(out _) ?? [];
        if(GamepadsOk(gamepads)) return;
        HashSet<uint> connected = [..GamepadLookup.Keys];
        foreach (var id in gamepads)
        {
            if (connected.Remove(id)) continue;
            AddGamepad(id);
        }
        foreach (var id in connected)
        {
            RemoveGamepad(id);
        }
    }
    private int AddGamepad(uint sdlId)
    {
        nint handle = SDL.OpenGamepad(sdlId);
        ErGamepad gamepad = new(handle, sdlId);
        GamepadLookup.Add(gamepad.SdlId, gamepad);
        for (int idx = 0; idx < Gamepads.Count; idx++)
        {
            if(Gamepads[idx] is not null) continue;
            Gamepads[idx] = gamepad;
            OnGamepadConnect(idx);
            return idx;
        }
        Gamepads.Add(gamepad);
        int gamepadIdx = Gamepads.Count-1;
        OnGamepadConnect(gamepadIdx);
        return gamepadIdx;
    }
    private int RemoveGamepad(uint sdlId)
    {
        var gamepad = GamepadLookup[sdlId];
        GamepadLookup.Remove(sdlId);
        int gamepadIdx = Gamepads.IndexOf(gamepad);
        OnGamepadDisconnect(gamepadIdx);

        Gamepads[gamepadIdx] = null;
        SDL.CloseGamepad(gamepad.Handle);
        return gamepadIdx;
    }
    private bool GamepadsOk(uint[] gamepadIds)
    {
        if(GamepadLookup.Count != gamepadIds.Length) return false;
        for (int idx = 0; idx < gamepadIds.Length; idx++)
        {
            if(!GamepadLookup.ContainsKey(gamepadIds[idx])) return false;
        }
        return true;
    }
}
