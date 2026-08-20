using ErisMath;

namespace ErisPhysics2D.Collider;

public abstract class ErColliderBody: ErCollider
{
    // public int ParentId;
    public ErVec2 Velocity;
    public override void Copy<T>(ref T value)
    {
        if(value is not ErColliderBody body) throw new("bad bod");
        base.Copy(ref value);
        // body.ParentId = ParentId;
        body.Velocity = Velocity;
    }
    public virtual void OnMove(){}
}
