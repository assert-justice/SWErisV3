namespace SpoonWitch.Utils;

public class SwClockGroup
{
    private readonly List<double> Clocks = [];
    public double this[int idx]
    {
        get
        {
            if(idx >= Clocks.Count) return 0;
            return Clocks[idx];
        }
        set
        {
            while(idx >= Clocks.Count) Clocks.Add(0);
            Clocks[idx] = value;
        }
    }
    public void Reset()
    {
        for (int idx = 0; idx < Clocks.Count; idx++)
        {
            Clocks[idx] = 0;
        }
    }
}