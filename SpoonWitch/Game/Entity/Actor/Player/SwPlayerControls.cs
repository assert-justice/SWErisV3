using Eris;
using Eris.Input;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Data;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.Actor.Player;

public class SwPlayerControls : SwComponent
{
    public readonly SwPlayerInput InputDevice = new();
    public SwPlayerControls(SwPlayer parent): base(parent, "controls"){}
    public override void Ready()
    {
        base.Ready();
        // InputDevice.SetProfileAll(SwData.Settings.Get("input_binds"));
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        InputDevice.Poll();
    }
}
