#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

internal class EnemyManager
{
    public const int EnemySpawnRange = 30;

    /// <summary>필드에 동시에 존재할 수 있는 적 상한.</summary>
    public const int MaxEnemies = 150;

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

    /// <summary>상한(150)일 때 10초마다 2배가 누적되는 스폰/강화 배율.</summary>
    private double _enemyStressMultiplier = 1.0;
    private double _secondsAccumulatedAtEnemyCap;

    private static PackedScene LoadEnemyPrefab(EnemyClass enemyClass, string path)
    {
        var prefab = GD.Load<PackedScene>(path)
                     ?? throw new InvalidOperationException($"적 프리팹을 불러올 수 없습니다: {path}");
        return prefab;
    }

    public EnemyManager(GameManager gameManager)
    {
        _gameManager = gameManager;

        _enemyPrefabs = new()
        {
            { EnemyClass.Minion, LoadEnemyPrefab(EnemyClass.Minion, "res://Prefabs/Enemies/enemy_minion.tscn") },
            { EnemyClass.Warrior, LoadEnemyPrefab(EnemyClass.Warrior, "res://Prefabs/Enemies/enemy_warrior.tscn") },
            { EnemyClass.Archer, LoadEnemyPrefab(EnemyClass.Archer, "res://Prefabs/Enemies/enemy_archer.tscn") },
            { EnemyClass.Mage, LoadEnemyPrefab(EnemyClass.Mage, "res://Prefabs/Enemies/enemy_mage.tscn") },
            { EnemyClass.Boss, LoadEnemyPrefab(EnemyClass.Boss, "res://Prefabs/Enemies/enemy_boss.tscn") },
        };
    }

    public void _PhysicsProcess(double delta)
    {
        foreach (var enemy in Enemies.Where(enemy => !enemy.GetTree().Paused))
        {
            var playerPos = _gameManager.Player.GlobalPosition;
            var direction = (playerPos - enemy.GlobalPosition).LimitLength();

            // enemy.LinearVelocity = direction * enemy.MovementSpeed;
        }
    }

    internal Enemy? SpawnEnemy() => SpawnEnemy(_enemyClasses[GD.RandRange(0, _enemyClasses.Count - 1)]);

    internal Enemy? SpawnEnemy(EnemyClass enemyClass)
    {
        if (_enemies.Count >= MaxEnemies)
            return null;

        var enemy = _enemyPrefabs[enemyClass].Instantiate<Enemy>();
        enemy.Name = enemyClass.ToString();
        var lifepointsBase = enemy.Lifepoints * _gameManager.GetMaxEnemyLifepoints() + _lifepointsBonus;
        var damagesBase = enemy.Damages + _damageBonus;
        var speedBase = enemy.MovementSpeed + _movespeedBonus;
        var m = _enemyStressMultiplier;
        enemy.Lifepoints = Math.Max(1, Mathf.RoundToInt((float)(lifepointsBase * m)));
        enemy.Damages = (uint)Math.Max(1, Mathf.RoundToInt((float)(damagesBase * m)));
        enemy.MovementSpeed = Math.Max(0.01f, (float)(speedBase * m));
        var spawnPos = GetRandomPos();
        enemy.TreeExiting += () => KillEnemy(enemy);
        var mainScene = _gameManager.GetNode("/root/MainScene");
        mainScene.AddChild(enemy);
        enemy.SetDeferred(Node3D.PropertyName.GlobalPosition, spawnPos);
        _enemies.Add(enemy);
        enemy.Connect(Enemy.SignalName.OnEnemyHit, Callable.From<Enemy, int>(_gameManager.EnemyHit));
        return enemy;
    }

    /// <summary>적이 상한에 도달한 채로 있을 때 10초마다 필드의 모든 적 스펙을 2배로 올립니다.</summary>
    internal void ProcessEnemyCapDifficulty(double delta)
    {
        if (_enemies.Count >= MaxEnemies)
        {
            _secondsAccumulatedAtEnemyCap += delta;
            while (_secondsAccumulatedAtEnemyCap >= 10.0)
            {
                _secondsAccumulatedAtEnemyCap -= 10.0;
                _enemyStressMultiplier *= 2.0;
                foreach (var e in _enemies)
                {
                    e.Lifepoints = (int)Math.Clamp((long)e.Lifepoints * 2L, 1L, int.MaxValue);
                    e.Damages = (uint)Math.Clamp((ulong)e.Damages * 2UL, 1UL, uint.MaxValue);
                    e.MovementSpeed *= 2f;
                }
            }
        }
        else
            _secondsAccumulatedAtEnemyCap = 0;
    }

    internal Enemy? SpawnBoss() => SpawnEnemy(EnemyClass.Boss);

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
