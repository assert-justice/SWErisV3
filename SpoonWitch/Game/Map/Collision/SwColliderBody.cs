using Eris;
using ErisPhysics2D.Collider;
using SpoonWitch.Game.Entity.Actor;

namespace SpoonWitch.Game.Map.Collision;

public class SwColliderBody: ErColliderBody
{
    // public int Id;
    // public override void Copy<T>(ref T value)
    // {
    //     if(value is not SwColliderBody body) throw new($"bad collider type {value.GetType()}");
    //     base.Copy(ref value);
    //     body.Id = Id;
    // }
    // public override void OnMove()
    // {
    //     base.OnMove();
    //     if(!SwGame.Game.EntityLookup.TryGet<SwActor>(ParentId.ToString(), out var actor))
    //     {
    //         actor.Position = Position;
    //         actor.Velocity = Velocity;
    //     }
    // }
}