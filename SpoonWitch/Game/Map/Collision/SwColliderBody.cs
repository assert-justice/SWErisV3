using Eris;
using ErisPhysics2D.Collider;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderBody: ErColliderBody
{
    public int Head;
    public override void Copy<T>(ref T value)
    {
        if(value is not SwColliderBody body) throw new($"bad collider type {value.GetType()}");
        base.Copy(ref value);
        body.Head = Head;
    }
    public override void OnMove()
    {
        base.OnMove();
        SwGame.PatchEnt(Head, Rect.Center, Velocity);
    }
}