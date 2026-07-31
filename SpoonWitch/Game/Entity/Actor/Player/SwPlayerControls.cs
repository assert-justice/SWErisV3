using Eris;
using Eris.Input;
using ErisMath;
using SpoonWitch.ByteStream;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayerControls: SwComponent
{
    private const double GAMEPAD_CURSOR_DISTANCE = 64;
    private readonly ErVAxis2[] Axis2s = [];
    private ErVAxis2 MoveAxis => Axis2s[0];
    private ErVAxis2 AimAxis => Axis2s[1];
    public ErVec2 Move{get => MoveAxis.Vector;}
    public ErVec2 LnzMove{get; private set;} = ErVec2.Right;
    public ErVec2 LastFacing{get; private set;} = ErVec2.Right;
    public int LastFacingIdx{get; private set;}
    public ErVec2 Aim{get; private set;}
    public ErVec2 LnzAim{get; private set;} = ErVec2.Right;
    public ErVec2 ReticlePosition{get; private set;}
    public bool ReticleVisible{get; private set;}
    private readonly ErVButton[] Buttons = [];
    private ErVButton Attack => Buttons[0];
    private ErVButton Fire => Buttons[1];
    private ErVButton Charge => Buttons[2];
    private ErVButton Dodge => Buttons[3];
    public bool AttackJustPressed{get => Attack.JustPressed;}
    public bool FireJustPressed{get => Fire.JustPressed;}
    public bool IsCharging{get; private set;}
    public bool DodgeJustPressed{get => Dodge.JustPressed;}
    public ErInputDevice Device = ErInputDevice.All();
    public SwPlayerControls(SwPlayer parent): base(parent, "controls")
    {
        if(!SwApp.TryLoadPrion("game_data/settings/default_input_settings.json", out var node))
        {
            ErEngine.LogWarning("no input bindings");
            return;
        }
        if(!ErInputProfile.TryGetAxes2(node, ["move", "aim"], out Axis2s)) return;
        if(!ErInputProfile.TryGetButtons(node, ["attack", "fire", "charge", "dodge"], out Buttons)) return;
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
    }
    public override void Update()
    {
        base.Update();
        foreach (var item in Axis2s)
        {
            item.Poll(Device);
        }
        foreach (var item in Buttons)
        {
            item.Poll(Device);
        }
        bool isGamepad = ErEngine.Input.LastEventDevice == ErInput.ErisInputDeviceKind.Gamepad;
        IsCharging = Charge.Pressed;
        if(!SwApp.Settings.TryGet("auto_charge", out bool auto_charge)) auto_charge = true;
        if(!SwApp.Settings.TryGet("kb_aiming", out bool kb_aiming)) kb_aiming = true;
        if(!SwApp.Settings.TryGet("reticle_always_visible_gp", out bool reticle_always_visible_gp)) reticle_always_visible_gp = false;
        if(!SwApp.Settings.TryGet("reticle_always_visible_kb", out bool reticle_always_visible_kb)) reticle_always_visible_kb = true;
        if (isGamepad)
        {
            if (Aim.IsNonzero())
            {
                Aim = AimAxis.Vector.Normalized();
                LnzAim = Aim;
                if(auto_charge) IsCharging = true;
                LastFacing = Aim;
            }
            else if(Move.IsNonzero()) LastFacing = Move.Normalized();
            ReticlePosition = Aim * GAMEPAD_CURSOR_DISTANCE;
            ReticleVisible = IsCharging || (Aim.IsNonzero() && reticle_always_visible_gp);
        }
        else
        {
            // Note: this is where we figure out where the mouse is relative to the player.
            var playerScreenPos = SwGame.PlayerPos - SwGame.Camera.Position;
            // Todo: make this less horrible
            ReticlePosition = ErEngine.Input.GetMousePosition() / (ErVec2)ErEngine.Renderer.WindowSize * SwApp.ScreenSize - SwApp.ScreenSize * 0.5 - playerScreenPos + new ErVec2(0, -SwApp.HUD_HEIGHT * 0.5);
            Aim = ReticlePosition.Normalized();
            // Note: If we're not charging and we're using keyboard aiming we aim in the last direction we moved as the aim vector.
            if(IsCharging || !kb_aiming) LastFacing = Aim;
            else if(Move.IsNonzero()) LastFacing = Move.Normalized();
            ReticleVisible = IsCharging || (Aim.IsNonzero() && reticle_always_visible_kb);
        }
        ReticleVisible = IsCharging || (Aim.IsNonzero() && reticle_always_visible_kb);
        LastFacingIdx = ErMath.RoundAngleToInt(LastFacing.GetAngle(), 4);
    }
}