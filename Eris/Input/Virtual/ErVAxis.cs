namespace Eris.Input.Virtual;

public class ErVAxis : ErBaseInput
{
    public double Value{get; private set;}
    public ErVAxis(string name) : base(name)
    {
    }
    public override void Poll()
    {
        throw new NotImplementedException();
    }

    public override void Clear()
    {
        throw new NotImplementedException();
    }
}
