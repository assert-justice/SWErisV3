using Eris.App;
using Eris.Input;
using Eris.Logging;
using Eris.Renderer;
using SDL3;

namespace Eris;

public static class ErEngine
{
    public static bool HasError{get; private set;}
    public static ErLogger Logger{get; private set;} = new();
    public static ErInput Input{get; private set;} = new();
    public static void SetLogger(ErLogger logger){Logger = logger;}
    public static void Log(params object?[] objects){Logger.Log(objects);}
    public static bool LogWarning(params object?[] objects){return Logger.LogWarning(objects);}
    public static bool LogError(params object?[] objects)
    {
        HasError = true;
        Logger.LogError(objects);
        Quit();
        return false;
    }
    private static readonly Stack<Action> CleanupStack = new();
    public static int Tickrate{get; private set;} = 90;
    public static ErRenderer Renderer{get; private set;} = new ErRenderer();
    public static void SetRenderer(ErRenderer renderer){
        if(!IsRunning) Renderer = renderer;
        else LogError("Cannot change renderer while engine is running");
    }
    public static bool IsRunning{get; private set;}
    public static double DeltaTime{get; private set;}
    public static double FrameDuration{get; private set;}
    public static double FrameTimeRemaining{get; private set;}
    public static double LastFrameTime{get; private set;}
    public static double CurrentTime{get; private set;}
    private static bool Init(IErApp app)
    {
        List<Action> initList = [
            Renderer.Init,
            ()=>CleanupStack.Push(Renderer.Cleanup),
            // ()=>Input = ErisInput.New(out _),
            app.Init,
            ()=>CleanupStack.Push(app.Cleanup),
        ];
        foreach (var action in initList)
        {
            action();
            if (HasError)
            {
                Cleanup();
                return false;
            }
        }
        return true;
    }
    private static void Cleanup()
    {
        while(CleanupStack.TryPop(out Action? result)) result();
    }
    public static double GetCurrentTime()
    {
        return SDL.GetPerformanceCounter() / (double)SDL.GetPerformanceFrequency();
    }
    public static void Run(IErApp app)
    {
        IsRunning = true;
        if(!Init(app)) return;
        LastFrameTime = GetCurrentTime();
        DeltaTime = 1 / (double)Tickrate;
        while (IsRunning)
        {
            double newTime = GetCurrentTime();
            FrameDuration = newTime - LastFrameTime;
            LastFrameTime = newTime;
            FrameTimeRemaining += FrameDuration;
            while(FrameTimeRemaining >= DeltaTime)
            {
                CurrentTime = GetCurrentTime();
                Input.Poll();
                app.Update();
                FrameTimeRemaining -= DeltaTime;
            }
            Renderer.BeginRender();
            app.Draw();
            Renderer.EndRender();
        }
        Cleanup();
    }
    public static void Quit(){IsRunning = false;}
}
