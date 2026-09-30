namespace SpoonWitch.Game.Entity.MapEntity;

public abstract class SwMapEntity: SwEntity
{
    public virtual void Unload()
    {
        QueueFree();
    }
}
