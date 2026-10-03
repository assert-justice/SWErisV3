namespace Eris.Input;

public class ErInputBuffer
{
    private struct ErState
    {
        public double Timestamp;
        public bool Down;
        public bool WasDown;
        public bool Used;
        public readonly bool JustDown => Down && !WasDown;
        public readonly double Elapsed => ErEngine.CurrentTime - Timestamp;
    }
    private readonly List<ErState> States = [];
    public void Advance()
    {
        for (int idx = 0; idx < States.Count; idx++)
        {
            var state = States[idx];
            state.WasDown = state.Down;
            state.Down = false;
            States[idx] = state;
        }
    }
    public void Poll()
    {
        for (int idx = 0; idx < States.Count; idx++)
        {
            var state = States[idx];
            if (state.JustDown)
            {
                state.Used = false;
                state.Timestamp = ErEngine.CurrentTime;
            }
            States[idx] = state;
        }
    }
    public void Set(int idx, bool down)
    {
        while(idx >= States.Count) States.Add(new());
        var state = States[idx];
        state.Down = down;
        States[idx] = state;
    }
    public bool GetDown(int idx)
    {
        if(idx >= States.Count) return false;
        return States[idx].Down;
    }
    public bool GetJustDown(int idx, double buffer)
    {
        if(idx >= States.Count) return false;
        var state = States[idx];
        if(!state.JustDown && state.Elapsed > buffer) return false;
        if(state.Used) return false;
        state.Used = true;
        States[idx] = state;
        return true;
    }
    public void Process(IEnumerable<int> indices)
    {
        Advance();
        foreach (var item in indices)
        {
            Set(item, true);
        }
        Poll();
    }
}
