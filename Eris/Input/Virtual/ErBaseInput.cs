namespace Eris.Input.Virtual;

public abstract class ErBaseInput
{
    public readonly string Name;
    public ErBaseInput(string name)
    {
        Name = name;
    }
    public abstract void Poll();
    public abstract void Clear();
}
