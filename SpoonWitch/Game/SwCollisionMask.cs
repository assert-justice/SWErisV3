namespace SpoonWitch.Game;

public enum SwCollisionMask: uint
{
    None = 0,
    BlocksNav = 1,
    IsOpaque = 2,
    Solid = 3,
    PlayerTeam = 4,
    Player = 5,
    EnemyTeam = 8,
    Enemy = 9,
}
