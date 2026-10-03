namespace ErisMath;

public class ErRandom
{
    private ulong _Seed;
    public ulong Seed => _Seed;
    public ErRandom()
    {
        _Seed = (ulong)DateTime.UtcNow.Ticks;
    }
    public ErRandom(ulong seed)
    {
        _Seed = seed;
    }
    public void SetSeed(ulong seed)
    {
        _Seed = seed;
    }
    public double Random()
    {
        return (double)SplitMix64(ref _Seed) / ulong.MaxValue;
    }
    // Note: max is not inclusive
    public int RandRange(int min, int max)
    {
        if(min > max)
        {
            (min,max) = (max,min);
        }
        int range = max - min;
        return ErMath.FloorToInt(Random() * range) + min;
    }
    public T PickRandom<T>(IList<T> list)
    {
        return PickRandom(list, out _);
    }
    public T PickRandom<T>(IList<T> list, out int idx)
    {
        idx = RandRange(0, list.Count);
        return list[idx];
    }
    public static ulong SplitMix64(ref ulong state)
    {
        // Adapted from pseudocode from https://rosettacode.org/wiki/Pseudo-random_numbers/Splitmix64
        state += 0x9e3779b97f4a7c15;              /* increment the state variable */
        ulong z = state;                          /* copy the state to a working variable */
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9; /* xor the variable with the variable right bit shifted 30 then multiply by a constant */
        z = (z ^ (z >> 27)) * 0x94d049bb133111eb; /* xor the variable with the variable right bit shifted 27 then multiply by a constant */
        return z ^ (z >> 31);                     /* divide by 2^64 to return a value between 0 and 1 */
    }
}
