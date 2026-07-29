using Prion.Node;

namespace SpoonWitch.Command;

public readonly struct SwCommand
{
    public readonly string Verb;
    public readonly PriNode Payload;
    public readonly long Timestamp;
    public readonly int TargetId;
    public SwCommand(string verb, PriNode payload, int targetId = -1)
    {
        Verb = verb;
        Payload = payload;
        Timestamp = DateTime.UtcNow.Ticks;
        TargetId = targetId;
    }
}