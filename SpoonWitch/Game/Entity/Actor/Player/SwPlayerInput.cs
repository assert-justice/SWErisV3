using Eris;
using Eris.Input;
using Eris.Input.Virtual;
using ErisMath;
using Prion.Node;
using SDL3;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayerInput: ErInputDevice
{
    private const double GAMEPAD_RETICLE_DISTANCE = 64;
    private ErVButton? Attack;
    private ErVButton? Fire;
    private ErVButton? Charge;
    private ErVButton? Dodge;
    private ErVButton? Use;
    private ErVButton? Heal;
    private ErVButton? Quaff;
    private ErVButton? LastPotion;
    private ErVButton? NextPotion;
    private ErVButton? Cast;
    private ErVButton? LastSpell;
    private ErVButton? NextSpell;
    private ErVButton? Pause;
    private ErVButton? UiCancel;
    private ErVButton? UiConfirm;
    private ErVButton? UiUp;
    private ErVButton? UiDown;
    private ErVButton? UiLeft;
    private ErVButton? UiRight;
    private ErVAxis2? MoveAxis;
    private ErVAxis2? AimAxis;
    private ErInput.DeviceKind _DeviceKind;
    private bool UseMostRecentDevice;
    public bool AutoCharge{get; private set;}
    public bool KbAiming{get; private set;}
    public ErInput.DeviceKind DeviceKind => UseMostRecentDevice ? ErEngine.Input.LastEventDevice : _DeviceKind;
    public ErVec2 Move => MoveAxis?.Vector ?? ErVec2.Zero;
    public ErVec2 LastMove{get; private set;} = ErVec2.Down;
    public ErVec2 Aim{get; private set;}
    public ErVec2 LastAim{get; private set;} = ErVec2.Down;
    public ErVec2 LastFacing{get; private set;} = ErVec2.Down;
    public int LastFacingIdx => ErMath.RoundAngleToInt(LastFacing.GetAngle(), 4);
    public bool AttackJustPressed => Attack?.JustPressed ?? false;
    public bool FireJustPressed => Fire?.JustPressed ?? false;
    public bool IsCharging => GetIsCharging();
    public bool DodgeJustPressed => Dodge?.JustPressed ?? false;
    public bool UseJustPressed => Use?.JustPressed ?? false;
    public bool HealJustPressed => Heal?.JustPressed ?? false;
    public bool QuaffJustPressed => Quaff?.JustPressed ?? false;
    public bool LastPotionJustPressed => LastPotion?.JustPressed ?? false;
    public bool NextPotionJustPressed => NextPotion?.JustPressed ?? false;
    public bool CastJustPressed => Cast?.JustPressed ?? false;
    public bool LastSpellJustPressed => LastSpell?.JustPressed ?? false;
    public bool NextSpellJustPressed => NextSpell?.JustPressed ?? false;
    public bool PauseJustPressed => Pause?.JustPressed ?? false;
    public bool UiCancelJustPressed => UiCancel?.JustPressed ?? false;
    public bool UiConfirmJustPressed => UiConfirm?.JustPressed ?? false;
    public bool UiUpJustPressed => UiUp?.JustPressed ?? false;
    public bool UiDownJustPressed => UiDown?.JustPressed ?? false;
    public bool UiLeftJustPressed => UiLeft?.JustPressed ?? false;
    public bool UiRightJustPressed => UiRight?.JustPressed ?? false;
    public ErVec2 PlayerScreenPosition;
    public ErVec2 ReticlePosition{get; private set;}
    public bool IsReticleVisible{get; private set;}
    private bool GetIsCharging()
    {
        if(Charge is not null && Charge.Pressed) return true;
        if(DeviceKind == ErInput.DeviceKind.Gamepad && AutoCharge && Aim.IsNonzero()) return true;
        return false;
    }
    private void SetAim()
    {
        if(DeviceKind == ErInput.DeviceKind.Kbm)
        {
            ErVec2 mousePos = ErEngine.Input.GetMousePosition(); // mouse position in window pixels
            var windowSize = (ErVec2)ErEngine.Renderer.WindowSize; // window size in pixels
            ErVec2 screenSize = SwApp.ScreenSize; // screen size in screen pixels
            mousePos *= screenSize / windowSize; // convert mouse position from window pixels to screen pixels
            mousePos -= screenSize / 2; // make the mouse pos relative to the center of the screen
            mousePos -= PlayerScreenPosition; // make the mouse pos relative to the player
            mousePos += new ErVec2(0, SwApp.HUD_HEIGHT / 2); // compensate for the hud
            ReticlePosition = mousePos;
            Aim = mousePos.Normalized();
            IsReticleVisible = true;
        }
        else
        {
            Aim = AimAxis?.Vector ?? ErVec2.Zero;
            ReticlePosition = Aim * GAMEPAD_RETICLE_DISTANCE;
            IsReticleVisible = Aim.IsNonzero();
        }
    }
    public override void Poll()
    {
        base.Poll();
        SetAim();
        bool anz = Aim.IsNonzero();
        bool mnz = Move.IsNonzero();
        if (anz)
        {
            LastAim = Aim;
        }
        if(mnz)
        {
            LastMove = Move;
        }
        // if we're using a keyboard and kb_aiming, only update last facing to last aim if the player is charging
        if(DeviceKind == ErInput.DeviceKind.Kbm && KbAiming)
        {
            if(anz && IsCharging) LastFacing = LastAim;
            else if(mnz) LastFacing = LastMove;
        }
        else
        {
            if(anz) LastFacing = LastAim;
            else if(mnz) LastFacing = LastMove;
        }
    }
    public void SetProfile(PriNode profile)
    {
        UseMostRecentDevice = true;
        var gamepad = profile.Get("gamepad");
        if(gamepad.TryGet("auto_charge", out bool b)) AutoCharge = b;
        var kbm = profile.Get("kbm");
        if(kbm.TryGet("kb_aiming", out b)) KbAiming = b;
        Buttons.Clear();
        Axes2.Clear();
        BindKbm(profile);
        BindGamepad(profile);
        SetInputs();
    }
    public void SetProfileKbm(PriNode profile)
    {
        UseMostRecentDevice = false;
        _DeviceKind = ErInput.DeviceKind.Kbm;
        var kbm = profile.Get("kbm");
        if(kbm.TryGet("kb_aiming", out bool b)) KbAiming = b;
        Buttons.Clear();
        Axes2.Clear();
        BindKbm(profile);
        SetInputs();
    }
    public void SetProfileGamepad(PriNode profile, int gamepadIdx = -1)
    {
        UseMostRecentDevice = false;
        _DeviceKind = ErInput.DeviceKind.Gamepad;
        var gamepad = profile.Get("gamepad");
        if(gamepad.TryGet("auto_charge", out bool b)) AutoCharge = b;
        Buttons.Clear();
        Axes2.Clear();
        BindGamepad(profile, gamepadIdx);
        SetInputs();
    }
    private void SetInputs()
    {
        Buttons.TryGetValue("attack", out Attack);
        Buttons.TryGetValue("fire", out Fire);
        Buttons.TryGetValue("charge", out Charge);
        Buttons.TryGetValue("dodge", out Dodge);
        Buttons.TryGetValue("use", out Use);
        Buttons.TryGetValue("heal", out Heal);
        Buttons.TryGetValue("quaff", out Quaff);
        Buttons.TryGetValue("last_potion", out LastPotion);
        Buttons.TryGetValue("next_potion", out NextPotion);
        Buttons.TryGetValue("cast", out Cast);
        Buttons.TryGetValue("last_spell", out LastSpell);
        Buttons.TryGetValue("next_spell", out NextSpell);
        Buttons.TryGetValue("pause", out Pause);
        Buttons.TryGetValue("ui_confirm", out UiConfirm);
        Buttons.TryGetValue("ui_cancel", out UiCancel);
        Buttons.TryGetValue("ui_up", out UiUp);
        Buttons.TryGetValue("ui_down", out UiDown);
        Buttons.TryGetValue("ui_left", out UiLeft);
        Buttons.TryGetValue("ui_right", out UiRight);
        Axes2.TryGetValue("move", out MoveAxis);
        Axes2.TryGetValue("aim", out AimAxis);
    }
    private void BindKbm(PriNode profile)
    {
        if(profile.TryGet("buttons", out PriDict dict))
        {
            foreach (var (key,value) in dict.Data)
            {
                if(!Buttons.TryGetValue(key, out var button))
                {
                    button = new(key);
                    Buttons.Add(button.Name, button);
                }
                if(value.TryGet("buffer", out double d)) button.Buffer = d;
                if(value.TryGet("deadzone", out d)) button.Deadzone = d;
                foreach (var item in value.Get("keys").Values)
                {
                    if(!TryAsEnum(item, out SDL.Scancode code)) continue;
                    button.Keys.Add(code);
                }
                foreach (var item in value.Get("mouse_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.MouseButtonFlags code)) continue;
                    button.MouseButtons.Add(code);
                }
            }
        }
        if(profile.TryGet("axes2", out dict))
        {
            foreach (var (key,value) in dict.Data)
            {
                if(!Axes2.TryGetValue(key, out var axis2))
                {
                    axis2 = new(key);
                    Axes2.Add(axis2.Name, axis2);
                }
                if(value.TryGet("deadzone", out double d)) axis2.Deadzone = d;
                var x = value.Get("x");
                foreach (var item in x.Get("neg_keys").Values)
                {
                    if(!TryAsEnum(item, out SDL.Scancode code)) continue;
                    axis2.XNegKeys.Add(code);
                }
                foreach (var item in x.Get("pos_keys").Values)
                {
                    if(!TryAsEnum(item, out SDL.Scancode code)) continue;
                    axis2.XPosKeys.Add(code);
                }
                var y = value.Get("y");
                foreach (var item in y.Get("neg_keys").Values)
                {
                    if(!TryAsEnum(item, out SDL.Scancode code)) continue;
                    axis2.YNegKeys.Add(code);
                }
                foreach (var item in y.Get("pos_keys").Values)
                {
                    if(!TryAsEnum(item, out SDL.Scancode code)) continue;
                    axis2.YPosKeys.Add(code);
                }
            }
        }
    }
    private void BindGamepad(PriNode profile, int gamepadIdx = -1)
    {
        if(profile.TryGet("buttons", out PriDict dict))
        {
            foreach (var (key,value) in dict.Data)
            {
                if(!Buttons.TryGetValue(key, out var button))
                {
                    button = new(key);
                    Buttons.Add(button.Name, button);
                }
                button.GamepadIdx = gamepadIdx;
                if(value.TryGet("buffer", out double d)) button.Buffer = d;
                if(value.TryGet("deadzone", out d)) button.Deadzone = d;
                foreach (var item in value.Get("gamepad_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadButton code)) continue;
                    button.GamepadButtons.Add(code);
                }
                foreach (var item in value.Get("gamepad_axes_low").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadAxis code)) continue;
                    button.GamepadAxesLow.Add(code);
                }
                foreach (var item in value.Get("gamepad_axes_high").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadAxis code)) continue;
                    button.GamepadAxesHigh.Add(code);
                }
            }
        }
        if(profile.TryGet("axes2", out dict))
        {
            foreach (var (key,value) in dict.Data)
            {
                if(!Axes2.TryGetValue(key, out var axis2))
                {
                    axis2 = new(key);
                    Axes2.Add(axis2.Name, axis2);
                }
                axis2.GamepadIdx = gamepadIdx;
                if(value.TryGet("deadzone", out double d)) axis2.Deadzone = d;
                var x = value.Get("x");
                foreach (var item in x.Get("neg_gamepad_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadButton code)) continue;
                    axis2.XNegGamepadButtons.Add(code);
                }
                foreach (var item in x.Get("pos_gamepad_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadButton code)) continue;
                    axis2.XPosGamepadButtons.Add(code);
                }
                foreach (var item in x.Get("gamepad_axes").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadAxis code)) continue;
                    axis2.XGamepadAxes.Add(code);
                }
                var y = value.Get("y");
                foreach (var item in y.Get("neg_gamepad_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadButton code)) continue;
                    axis2.YNegGamepadButtons.Add(code);
                }
                foreach (var item in y.Get("pos_gamepad_buttons").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadButton code)) continue;
                    axis2.YPosGamepadButtons.Add(code);
                }
                foreach (var item in y.Get("gamepad_axes").Values)
                {
                    if(!TryAsEnum(item, out SDL.GamepadAxis code)) continue;
                    axis2.YGamepadAxes.Add(code);
                }
            }
        }
    }
    private static bool TryAsEnum<TEnum>(PriNode node, out TEnum value) where TEnum: struct
    {
        value = default;
        if(!node.TryAs(out string s)) return ErEngine.LogWarning("enum parse failed, not a string: ", node);
        if(!Enum.TryParse(s, out value)) return ErEngine.LogWarning("enum parse failed for string '", s ,"'");
        return true;
    }
}
