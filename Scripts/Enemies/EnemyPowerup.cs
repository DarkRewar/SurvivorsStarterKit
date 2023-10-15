using Godot;

public enum EnemyPowerupType
{
    UnlockClassWarrior = 0,
    UnlockClassMage = 1,
    UnlockClassArcher = 2,

    BossSpawn = 50,

    Lifepoints = 100,
    Damages = 101,
    Movespeed = 102,
    SpawnRate = 103,
    Bomb = 104,
}

[GlobalClass]
public partial class EnemyPowerup : Resource
{
    [Export]
    public string Name { get; private set; }

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; }

    [Export]
    public int MaxStack { get; private set; } = 1;

    [Export]
    public EnemyPowerupType Type { get; private set; }
}
