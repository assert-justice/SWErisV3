namespace SpoonWitch.Game.Entity;

public interface ISwEntity<T> where T: SwEntity
{
    public abstract static byte TypeId{get;}
    public abstract static T Primary{get;}
    public abstract static T Secondary{get;}
}