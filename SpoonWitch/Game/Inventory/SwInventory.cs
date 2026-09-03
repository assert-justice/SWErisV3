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
    public void SetCount(string key, int count)
    {
        if(Data.TryGetValue(key, out var value))
        {
            value.Count = count;
        }
        else Data[key] = new(count);
    }
    public void SetCount(string key, int count, int max)
    {
        if(Data.TryGetValue(key, out var value))
        {
            value.Max = max;
            value.Count = count;
        }
        else Data[key] = new(count, max);
    }
}
