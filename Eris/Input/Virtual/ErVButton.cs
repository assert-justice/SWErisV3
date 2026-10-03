using SDL3;

namespace Eris.Input.Virtual;

public class ErVButton : ErBaseInput
{
    public readonly List<SDL.Scancode> Keys = [];
    public readonly List<SDL.MouseButtonFlags> MouseButtons = [];
    public readonly List<SDL.GamepadButton> GamepadButtons = [];
    public readonly List<SDL.GamepadAxis> GamepadAxesLow = [];
    public readonly List<SDL.GamepadAxis> GamepadAxesHigh = [];
    // public double PulseDelay = double.PositiveInfinity;
    // public double PulseCooldown = double.PositiveInfinity;
    public double Buffer = 0;
    public bool Pressed => GetPressed();
    public bool JustPressed => GetJustPressed();
    public int GamepadIdx = -2;
    private bool State = false;
    private bool LastState = false;
    private double LastChangeTime;
    public double Elapsed => ErEngine.CurrentTime - LastChangeTime;
    // private bool JustPulsed = false;
    private bool BufferEnabled = false;
    public ErVButton(string name) : base(name)
    {
    }
    public override void Poll()
    {
        State = GetState();
        if(State != LastState)
        {
            LastChangeTime = ErEngine.CurrentTime;
            LastState = State;
            if(State) BufferEnabled = true;
        }
        // else HandlePulse();
    }
    public override void Clear()
    {
        Keys.Clear();
        MouseButtons.Clear();
        GamepadButtons.Clear();
        GamepadAxesHigh.Clear();
        GamepadAxesLow.Clear();
    }
    private bool HandleState()
    {
        bool res = false;
        foreach (var key in Keys)
        {
            if (ErEngine.Input.HandleKeyDown(key)) res = true;
        }
        foreach (var mb in MouseButtons)
        {
            if(ErEngine.Input.HandleMouseButtonDown(mb)) res = true;
        }
        foreach (var button in GamepadButtons)
        {
            if(ErEngine.Input.HandleGamepadButtonDown(button, GamepadIdx)) res = true;
        }
        foreach (var axis in GamepadAxesLow)
        {
            if(ErEngine.Input.HandleGamepadAxis(axis, GamepadIdx) < -ErEngine.Input.GlobalAxisDeadzone) res = true;
        }
        foreach (var axis in GamepadAxesHigh)
        {
            if(ErEngine.Input.HandleGamepadAxis(axis, GamepadIdx) > ErEngine.Input.GlobalAxisDeadzone) res = true;
        }
        return res;
    }
    private bool GetState()
    {
        bool res = false;
        foreach (var key in Keys)
        {
            if (ErEngine.Input.GetKeyDown(key)) res = true;
        }
        foreach (var mb in MouseButtons)
        {
            if(ErEngine.Input.GetMouseButtonDown(mb)) res = true;
        }
        foreach (var button in GamepadButtons)
        {
            if(ErEngine.Input.GetGamepadButtonDown(button, GamepadIdx)) res = true;
        }
        foreach (var axis in GamepadAxesLow)
        {
            if(ErEngine.Input.GetGamepadAxis(axis, GamepadIdx) < -ErEngine.Input.GlobalAxisDeadzone) res = true;
        }
        foreach (var axis in GamepadAxesHigh)
        {
            if(ErEngine.Input.GetGamepadAxis(axis, GamepadIdx) > ErEngine.Input.GlobalAxisDeadzone) res = true;
        }
        return res;
    }
    // private void HandlePulse(){}
    private bool GetPressed()
    {
        if(!State) return false;
        HandleState();
        return State;
    }
    private bool GetJustPressed()
    {
        if(!State) return false;
        if(!BufferEnabled) return false;
        if(Elapsed > Buffer) return false;
        HandleState();
        return true;
    }
}

// public class ErVButton
// {
//     public class ErState
//     {
//         public bool Pressed;
//         public bool LastPressed;
//         public double TimeLastChanged;
//         public bool Pulsed;
//         public double Duration;
//     }
//     private readonly double Buffer = 0;
//     private readonly int GamepadId = -1;
//     public ErState State = new();
//     public double PressedDuration => State.Pressed ? ErEngine.CurrentTime - State.TimeLastChanged : 0;
//     public double ReleasedDuration => !State.Pressed ? ErEngine.CurrentTime - State.TimeLastChanged : 0;
//     public bool Pressed{get => State.Pressed;}
//     public bool JustPressed
//     {
//         get
//         {
//             double now = ErEngine.LastFrameTime;
//             if((State.Pressed && !State.LastPressed) || State.Pulsed || now - State.TimeLastChanged < Buffer)
//             {
//                 State.TimeLastChanged = now - Buffer;
//                 return true;
//             }
//             return false;
//         }
//     }
//     public bool JustReleased{get => !State.Pressed && State.LastPressed;}
//     public ErVButton(){}
//     public ErVButton(PriNode data)
//     {
//         if(data.TryGet("pulse_delay", out double d)) PulseDelay = d;
//         if(data.TryGet("pulse_cooldown", out d)) PulseCooldown = d;
//         if(data.TryGet("buffer", out d)) Buffer = d;
//         Keys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(data.Get("keys"))];
//         MouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(data.Get("mouse_buttons"))];
//         GamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(data.Get("gamepad_buttons"))];
//         GamepadAxesLow = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(data.Get("gamepad_axes_low"))];
//         GamepadAxesHigh = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(data.Get("gamepad_axes_high"))];
//     }
//     public void Poll(ErInputDevice device)
//     {
//         State.LastPressed = State.Pressed;
//         State.Pressed = GetPressed(device);
//         if(State.Pressed != State.LastPressed) State.TimeLastChanged = ErEngine.CurrentTime;
//         double dt = ErEngine.DeltaTime;
//         if(PressedDuration < PulseDelay || !State.Pressed)
//         {
//             State.Pulsed = false;
//             return;
//         }
//         double duration = PressedDuration - PulseDelay;
//         duration -= Math.Floor(duration / PulseCooldown) * PulseCooldown;
//         State.Pulsed = duration <= dt;
//     }
// }