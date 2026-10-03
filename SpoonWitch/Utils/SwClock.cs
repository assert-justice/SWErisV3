using Eris;

namespace SpoonWitch.Utils;

public class SwClock
{
    public double Duration{get; private set;} = 1;
    public double Remaining{get; private set;}
    public double Elapsed => Duration - Remaining;
    public double Progress => Elapsed / Duration;
    public bool IsPaused = false;
    public bool IsFinished => Remaining <= 0;
    public bool IsRunning => !IsPaused && !IsFinished;
    public Action OnFinish = ()=>{};
    public void Update(double dt)
    {
        if(!IsRunning) return;
        Remaining -= dt;
        if(!IsRunning) OnFinish();
    }
    public SwClock SetDuration(double duration)
    {
        if(duration < 0) {ErEngine.LogWarning("attempted to set negative duration"); return this;}
        Duration = duration;
        Remaining = 0;
        return this;
    }
    public SwClock Start()
    {
        return Restart();
    }
    public SwClock Start(double duration)
    {
        return SetDuration(duration).Start();
    }
    public SwClock Restart()
    {
        IsPaused = false;
        Remaining = Duration;
        return this;
    }
    public SwClock Pause()
    {
        IsPaused = true;
        return this;
    }
    public SwClock Resume()
    {
        IsPaused = false;
        return this;
    }
    public SwClock Stop()
    {
        IsPaused = false;
        Remaining = 0;
        return this;
    }
}
