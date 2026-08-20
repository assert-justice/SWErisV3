using Eris;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.State;

public class SwStateMachine: SwComponent
{
    private readonly SwState[] States;
    private readonly Dictionary<string, int> StateLookup = [];
    private int CurrentStateIdx = 0;
    private string NextState = string.Empty;
    private bool FirstUpdate = true;
    public SwState CurrentState{get => States[CurrentStateIdx];}
    public SwStateMachine(SwEntity parent, string name, IEnumerable<SwState> states): base(parent, name)
    {
        States = [..states];
        if(States.Length == 0) throw new Exception("passed empty array of states");
        NextState = States[0].Name;
        for (int idx = 0; idx < States.Length; idx++)
        {
            if(!StateLookup.TryAdd(States[idx].Name, idx)) throw new Exception($"duplicate state name '{States[idx]}'");
            States[idx].Init(this);
        }
    }
    public void SetState(string state)
    {
        // if (!string.IsNullOrEmpty(NextState))
        // {
        //     ErEngine.LogWarning("attempted to set state '", state, "' while state '", NextState, "' was already queued.");
        //     return;
        // }
        if(state == CurrentState.Name) return;
        if(!StateLookup.ContainsKey(state))
        {
            ErEngine.LogError("attempted to set invalid state '", state, "'.");
            return;
        }
        NextState = state;
    }
    public override void Update()
    {
        if (!string.IsNullOrEmpty(NextState))
        {
            if (FirstUpdate)
            {
                FirstUpdate = false;
            }
            else CurrentState.EndState(NextState);
            string lastState = CurrentState.Name;
            CurrentStateIdx = StateLookup[NextState];
            CurrentState.BeginState(lastState);
            NextState = string.Empty;
        }
        CurrentState.Update();
    }
    public override void Draw(SwComponent nextState)
    {
        base.Draw(nextState);
        if(nextState is not SwStateMachine machine) ErEngine.LogError("type mismatch, expected state machine, found'", nextState.GetType(), "'.");
        else CurrentState.Draw(machine.CurrentState);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
        foreach (var item in States)
        {
            item.Read(byteStream);
        }
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
        foreach (var item in States)
        {
            item.Write(byteStream);
        }
    }
}