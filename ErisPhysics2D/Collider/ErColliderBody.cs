using ErisMath;

namespace ErisPhysics2D.Collider;

public class ErColliderBody: ErCollider
{
    public ErVec2 Velocity;
    public override void Copy<T>(ref T value)
    {
        if(value is not ErColliderBody body) throw new("bad bod");
        base.Copy(ref value);
        body.Velocity = Velocity;
    }
    // public virtual void OnMove(){}
}
