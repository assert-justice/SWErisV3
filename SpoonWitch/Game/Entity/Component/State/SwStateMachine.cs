using Eris;
using SpoonWitch.ByteStream;

namespace SpoonWitch.Game.Entity.Component.State;

public class SwStateMachine<T>: SwComponent where T: SwEntity
{
    private readonly SwState<T>[] States;
    private readonly Dictionary<string, int> StateLookup = [];
    private int CurrentStateIdx = 0;
    private string NextState = string.Empty;
    private bool FirstUpdate = true;
    public SwState<T> CurrentState{get => States[CurrentStateIdx];}
    public string DefaultState{get; private set;}
    public T Entity;
    public SwStateMachine(T parent, string name, IEnumerable<SwState<T>> states): base(parent, name)
    {
        Entity = parent;
        States = [..states];
        if(States.Length == 0) throw new Exception("passed empty array of states");
        DefaultState = States[0].Name;
        NextState = DefaultState;
        for (int idx = 0; idx < States.Length; idx++)
        {
            if(!StateLookup.TryAdd(States[idx].Name, idx)) throw new Exception($"duplicate state name '{States[idx]}'");
            States[idx].Init(this);
        }
    }
    public override void Ready()
    {
        base.Ready();
        foreach (var item in States)
        {
            item.Ready();
        }
    }
    public void SetState(string state)
    {
        if(state == CurrentState.Name) return;
        if(!StateLookup.ContainsKey(state))
        {
            ErEngine.LogError("attempted to set invalid state '", state, "'.");
            return;
        }
        NextState = state;
    }
    public void SetDefaultState()
    {
        SetState(DefaultState);
    }
    public void SetDefaultState(string state)
    {
        if(state == DefaultState) return;
        if (!StateLookup.ContainsKey(state))
        {
            ErEngine.LogError("attempted to set invalid default state '", state, "'.");
            return;
        }
        DefaultState = state;
        SetDefaultState();
    }
    public override void Update(double dt)
    {
        base.Update(dt);
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
        CurrentState.Update(dt);
    }
    public override void Draw()
    {
        base.Draw();
        CurrentState.Draw();
    }
}