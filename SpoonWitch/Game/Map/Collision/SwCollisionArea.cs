using Eris;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderArea: ErColliderArea
{
    public override void OnBodyEnter(int bodyId, ErColliderBody body)
    {
        base.OnBodyEnter(bodyId, body);
        ErEngine.Log("body entered ", bodyId);
    }
    public override void OnBodyExit(int bodyId, ErColliderBody body)
    {
        base.OnBodyExit(bodyId, body);
        ErEngine.Log("body exited ", bodyId);
    }
}