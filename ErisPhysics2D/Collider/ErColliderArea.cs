namespace ErisPhysics2D.Collider;

public abstract class ErColliderArea: ErCollider
{
    private readonly HashSet<int> OverlappingBodyIds = [];
    private readonly List<int> BodyIds = [];
    internal void ClearBodies()
    {
        OverlappingBodyIds.Clear();
    }
    internal void AddBody(int id)
    {
        OverlappingBodyIds.Add(id);
    }
    public override void OnRemove()
    {
        base.OnRemove();
    }
    public virtual void OnBodyEnter(int bodyId, ErColliderBody body){}
    public virtual void OnBodyExit(int bodyId, ErColliderBody body){}
    public virtual void Update(IEnumerable<(int bodyId, ErColliderBody body)> bodies, Dictionary<int,ErColliderBody> bodyLookup)
    {
        // loop through the bodies we are colliding with. if they weren't previously in our overlapping ids, call on body enter. otherwise remove the id from the set
        foreach (var (bodyId, body) in bodies)
        {
            if(!OverlappingBodyIds.Remove(bodyId)) OnBodyEnter(bodyId, body);
            BodyIds.Add(bodyId);
        }
        // now the bodies we were colliding with have been removed from the overlapping set, all that remains is the bodies that exited. we try to find them and call on body exit if possible
        foreach (var bodyId in OverlappingBodyIds)
        {
            if(bodyLookup.TryGetValue(bodyId, out var body)) OnBodyExit(bodyId, body);
        }
        OverlappingBodyIds.Clear();
        foreach (var bodyId in BodyIds)
        {
            OverlappingBodyIds.Add(bodyId);
        }
        BodyIds.Clear();
    }
}
