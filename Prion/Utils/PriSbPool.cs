using System.Text;

namespace Prion.Utils;

public class PriSbPool
{
    private readonly Queue<StringBuilder> SbQueue = [];
    public StringBuilder Get()
    {
        if(!SbQueue.TryDequeue(out var sb)) return new();
        sb.Clear();
        return sb;
    }
    public string Free(StringBuilder stringBuilder)
    {
        SbQueue.Enqueue(stringBuilder);
        return stringBuilder.ToString();
    }
}