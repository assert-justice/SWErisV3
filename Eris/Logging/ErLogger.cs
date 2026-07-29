namespace Eris.Logging;

public class ErLogger
{
    public string Separator{get; set;} = "";
    public string EndLine{get; set;} = "\n";
    private readonly List<object?> LogBuffer = [];
    private readonly Stack<int> LogStack = [];
    public virtual void Write(object? arg, int indent = 0)
    {
        for (int idx = 0; idx < indent; idx++)
        {
            Console.Write('\t');
        }
        if(arg is null) Console.Write("null");
        else Console.Write(arg.ToString());
    }
    public virtual void WriteLine(object? arg, int indent = 0)
    {
        Write(arg, indent);
        Write(EndLine);
    }
    public virtual void BeginLog(params object?[] args)
    {
        LogStack.Push(LogBuffer.Count);
        LogBuffer.Clear();
        PushLog(args);
    }
    public virtual void PushLog(params object?[] args)
    {
        foreach (var arg in args)
        {
            LogBuffer.Add(arg);
        }
    }
    // public virtual void PushLine(params object?[] args)
    // {
    //     PushLog(args);
    //     PushLog(EndLine);
    // }
    public virtual object? PopLog()
    {
        if(LogBuffer.Count == 0) return null;
        var res = LogBuffer[^1];
        LogBuffer.RemoveAt(LogBuffer.Count-1);
        return res;
    }
    public virtual void CommitLog(params object?[] args)
    {
        PushLog(args);
        if(!LogStack.TryPop(out int start)) start = 0;
        if(LogBuffer.Count - start <= 0) return;
        Write(LogBuffer[start]);
        for (int idx = start + 1; idx < LogBuffer.Count; idx++)
        {
            Write(Separator);
            Write(LogBuffer[idx]);
        }
        Write(EndLine);
        LogBuffer.RemoveRange(start, LogBuffer.Count - start);
        // LogBuffer.Clear();
    }
    public virtual void CommitWarning(params object?[] args)
    {
        Write("WARNING: ");
        PushLog(args);
        CommitLog();
    }
    public virtual bool CommitError(params object?[] args)
    {
        Write("ERROR: ");
        PushLog(args);
        CommitLog();
        return false;
    }
    public virtual void Log(params object?[] args)
    {
        BeginLog(args);
        CommitLog();
    }
    public virtual bool LogWarning(params object?[] args)
    {
        BeginLog(args);
        CommitWarning();
        return false;
    }
    public virtual bool LogError(params object?[] args)
    {
        BeginLog(args);
        CommitError();
        return false;
    }
}