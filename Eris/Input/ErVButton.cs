using Prion.Node;
using SDL3;

namespace Eris.Input;

public class ErVButton
{
    private readonly SDL.Scancode[] Keys = [];
    private readonly SDL.MouseButtonFlags[] MouseButtons = [];
    private readonly SDL.GamepadButton[] GamepadButtons = [];
    private readonly SDL.GamepadAxis[] GamepadAxesLow = [];
    private readonly SDL.GamepadAxis[] GamepadAxesHigh = [];
    public readonly double PulseDelay = double.PositiveInfinity;
    public readonly double PulseCooldown = double.PositiveInfinity;
    public class ErState
    {
        public bool Pressed;
        public bool LastPressed;
        public double TimeLastChanged;
        public bool Pulsed;
        public double Duration;
    }
    private readonly double Buffer = 0;
    private readonly int GamepadId = -1;
    public ErState State = new();
    public double PressedDuration => State.Pressed ? ErEngine.CurrentTime - State.TimeLastChanged : 0;
    public double ReleasedDuration => !State.Pressed ? ErEngine.CurrentTime - State.TimeLastChanged : 0;
    public bool Pressed{get => State.Pressed;}
    public bool JustPressed
    {
        get
        {
            double now = ErEngine.LastFrameTime;
            if((State.Pressed && !State.LastPressed) || State.Pulsed || now - State.TimeLastChanged < Buffer)
            {
                State.TimeLastChanged = now - Buffer;
                return true;
            }
            return false;
        }
    }
    public bool JustReleased{get => !State.Pressed && State.LastPressed;}
    public ErVButton(){}
    public ErVButton(PriNode data)
    {
        if(data.TryGet("pulse_delay", out double d)) PulseDelay = d;
        if(data.TryGet("pulse_cooldown", out d)) PulseCooldown = d;
        if(data.TryGet("buffer", out d)) Buffer = d;
        Keys = [..ErInputProfile.GetEnumArray<SDL.Scancode>(data.Get("keys"))];
        MouseButtons = [..ErInputProfile.GetEnumArray<SDL.MouseButtonFlags>(data.Get("mouse_buttons"))];
        GamepadButtons = [..ErInputProfile.GetEnumArray<SDL.GamepadButton>(data.Get("gamepad_buttons"))];
        GamepadAxesLow = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(data.Get("gamepad_axes_low"))];
        GamepadAxesHigh = [..ErInputProfile.GetEnumArray<SDL.GamepadAxis>(data.Get("gamepad_axes_high"))];
    }
    private bool GetPressed(ErInputDevice device)
    {
        if (device.UseKeyboard)
        {
            foreach (var key in Keys)
            {
                if (ErEngine.Input.GetKeyDown(key)) return true;
            }
            foreach (var mb in MouseButtons)
            {
                if(ErEngine.Input.GetMouseButtonDown(mb)) return true;
            }
        }
        if (device.UseGamepad)
        {
            foreach (var button in GamepadButtons)
            {
                if(ErEngine.Input.GetGamepadButtonDown(button, GamepadId)) return true;
            }
            foreach (var axis in GamepadAxesLow)
            {
                if(ErEngine.Input.GetGamepadAxis(axis, GamepadId) < -ErEngine.Input.GlobalAxisDeadzone) return true;
            }
            foreach (var axis in GamepadAxesHigh)
            {
                if(ErEngine.Input.GetGamepadAxis(axis, GamepadId) > ErEngine.Input.GlobalAxisDeadzone) return true;
            }
        }
        return false;
    }
    public void Poll(ErInputDevice device)
    {
        State.LastPressed = State.Pressed;
        State.Pressed = GetPressed(device);
        if(State.Pressed != State.LastPressed) State.TimeLastChanged = ErEngine.CurrentTime;
        double dt = ErEngine.DeltaTime;
        if(PressedDuration < PulseDelay || !State.Pressed)
        {
            State.Pulsed = false;
            return;
        }
        double duration = PressedDuration - PulseDelay;
        duration -= Math.Floor(duration / PulseCooldown) * PulseCooldown;
        State.Pulsed = duration <= dt;
    }
}