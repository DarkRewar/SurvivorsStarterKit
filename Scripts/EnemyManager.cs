using Godot;
using System.Collections.Generic;
using System.Linq;

internal class EnemyManager
{
    public const int EnemySpawnRange = 30;

    public float SpawnRate { get; private set; } = 1;

    public float SpawnDelay => 1f / SpawnRate;

    private readonly GameManager _gameManager;
    private readonly Dictionary<EnemyClass, PackedScene> _enemyPrefabs;
    private readonly List<Enemy> _enemies = new();
    public List<Enemy> Enemies => _enemies;

    // BONUSES
    private List<EnemyClass> _enemyClasses = new() { EnemyClass.Minion };
    private int _lifepointsBonus = 0;
    private uint _damageBonus = 0;
    private float _movespeedBonus = 0;

    public EnemyManager(GameManager gameManager)
    {
        _gameManager = gameManager;

        _enemyPrefabs = new()
        {
            { EnemyClass.Minion, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_minion.tscn") },
            { EnemyClass.Warrior, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_warrior.tscn") },
            { EnemyClass.Archer, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_archer.tscn") },
            { EnemyClass.Mage, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_mage.tscn") },
            { EnemyClass.Boss, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_boss.tscn") },
        };
    }

    public void _PhysicsProcess(double delta)
    {
        foreach (var enemy in Enemies.Where(enemy => !enemy.GetTree().Paused))
        {
            var playerPos = _gameManager.Player.GlobalPosition;
            var direction = (playerPos - enemy.GlobalPosition).LimitLength();

            enemy.LinearVelocity = direction * enemy.MovementSpeed;
        }
    }

    internal Enemy SpawnEnemy() => SpawnEnemy(_enemyClasses[GD.RandRange(0, _enemyClasses.Count - 1)]);

    internal Enemy SpawnEnemy(EnemyClass enemyClass)
    {
        var enemy = _enemyPrefabs[enemyClass].Instantiate<Enemy>();
        enemy.Name = enemyClass.ToString();
        enemy.Lifepoints = enemy.Lifepoints * _gameManager.GetMaxEnemyLifepoints() + _lifepointsBonus;
        enemy.Damages += _damageBonus;
        enemy.MovementSpeed += _movespeedBonus;
        enemy.Position = GetRandomPos();
        enemy.TreeExiting += () => KillEnemy(enemy);
        _gameManager.GetNode("/root/MainScene").AddChild(enemy);
        _enemies.Add(enemy);
        enemy.Connect(Enemy.SignalName.OnEnemyHit, Callable.From<Enemy, int>(_gameManager.EnemyHit));
        return enemy;
    }

    internal Enemy SpawnBoss() => SpawnEnemy(EnemyClass.Boss);

    private void KillEnemy(Enemy enemy)
    {
        _enemies.Remove(enemy);
        _gameManager.EnemyKilled(enemy.Experience);
    }

    private Vector3 GetRandomPos() => _gameManager.GetRandomPosAroundPlayer(EnemySpawnRange);

    internal void Upgrade(EnemyPowerup enemyPowerup)
    {
        switch (enemyPowerup.Type)
        {
            case EnemyPowerupType.UnlockClassWarrior:
                _enemyClasses.Add(EnemyClass.Warrior);
                break;
            case EnemyPowerupType.UnlockClassMage:
                _enemyClasses.Add(EnemyClass.Mage);
                break;
            case EnemyPowerupType.UnlockClassArcher:
                _enemyClasses.Add(EnemyClass.Archer);
                break;
            case EnemyPowerupType.BossSpawn:
                SpawnBoss();
                break;
            case EnemyPowerupType.Lifepoints:
                _lifepointsBonus += (int)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Damages:
                _damageBonus += (uint)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Movespeed:
                _movespeedBonus += ((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.SpawnRate:
                SpawnRate += ((StatEnemyPowerup)enemyPowerup).Value;
                break;
            default:
                GD.PrintErr($"{enemyPowerup.Type} is not handled");
                break;
        }
    }

    internal double GetFinalValue(EnemyPowerup enemyPowerup) => enemyPowerup.Type switch
    {
        EnemyPowerupType.Lifepoints => _lifepointsBonus + (int)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Damages => _damageBonus + (uint)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Movespeed => (double)(_movespeedBonus + ((StatEnemyPowerup)enemyPowerup).Value),
        EnemyPowerupType.SpawnRate => (double)(1f / (SpawnRate + ((StatEnemyPowerup)enemyPowerup).Value)),
        _ => default,
    };
}
