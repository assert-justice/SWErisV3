using Eris;

namespace SpoonWitch.Game.Entity.Actor.Enemy.Slume;

public class SwSlume : SwEnemy, ISwEntity<SwSlume>
{
    public static byte TypeId => 1;
    private static SwSlume? _Primary;
    private static SwSlume? _Secondary;
    public static SwSlume Primary => _Primary ??= new();
    public static SwSlume Secondary => _Secondary ??= new();
    protected override byte GetTypeId => TypeId;
    public SwSlume()
    {
        string path = "game_data/entities/actors/slume/slume_anim_data.json";
        if(!TryLoadSprites(path)) ErEngine.LogWarning("failed to load slume sprites");
        // RegisterComponent(SwPlayerState.GetPlayerStateMachine(this, "state_machine"));
    }
}