using ErisPhysics2D.Collider;

public class SwColliderBody : ErColliderBody
{
    public readonly int ParentId;
    public SwColliderBody(int id, int parentId) : base(id)
    {
        ParentId = parentId;
    }
}
