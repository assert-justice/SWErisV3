namespace SpoonWitch.Game.Inventory;

public class SwInventory
{
    private struct Entry(int count, int? max = null, int? min = null)
    {
        private int _Count = count;
        private int _Max = max ?? int.MaxValue;
        private int _Min = min ?? 0;
        public int Count
        {
            readonly get => _Count;
            set => _Count = Math.Clamp(value, Min, Max);
        }
        public int Max
        {
            readonly get => _Max;
            set
            {
                _Max = value;
                Count = _Count;
            }
        }
        public int Min
        {
            readonly get => _Min;
            set
            {
                _Min = value;
                Count = _Count;
            }
        }
    }
    private readonly Dictionary<string, Entry> Data = [];
    public int GetCount(string key)
    {
        if(!Data.TryGetValue(key, out var entry)) return 0;
        return entry.Count;
    }
    public int GetMax(string key)
    {
        if(!Data.TryGetValue(key, out var entry)) return 0;
        return entry.Max;
    }
    public void SetCount(string key, int count)
    {
        if(Data.TryGetValue(key, out var value))
        {
            value.Count = count;
            Data[key] = value;
        }
        else Data[key] = new(count);
    }
    public void SetCount(string key, int count, int max)
    {
        if(Data.TryGetValue(key, out var value))
        {
            value.Max = max;
            value.Count = count;
            Data[key] = value;
        }
        else Data[key] = new(count, max);
    }
    public bool TryAdd(string key, int count, out int rem)
    {
        rem = default;
        if(!Data.TryGetValue(key, out var entry)) return false;
        int room = entry.Max - entry.Count;
        if(room <= 0) return false;
        int newCount = entry.Count + count;
        if(newCount > entry.Max) rem = newCount - entry.Max;
        entry.Count = newCount - rem;
        Data[key] = entry;
        return true;
    }
}
