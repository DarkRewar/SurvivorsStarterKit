using Godot;
using System.Collections.Generic;

public partial class SpiritWater : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    [Export]
    public float Duration = 4f;

    private float _durationBonus = 0;

    public float TotalDuration => Duration + _durationBonus;

    [Export]
    public float Cooldown = 5f;

    private float _cooldownBonus = 0;

    public float TotalCooldown => Cooldown - _cooldownBonus;

    [Export]
    public float ProjectileRange = 2;

    [Export]
    public PackedScene ProjectilePrefab;
    private Timer _projectileCooldown;
    private Timer _damageCooldown;

    private List<Enemy> _enemies = new();
    private GameManager _gameManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _projectileCooldown = GetNode<Timer>("ProjectileCooldown");
        _projectileCooldown.WaitTime = TotalCooldown;
        _projectileCooldown.Start();
        _projectileCooldown.Timeout += OnAttackReady;

        _damageCooldown = GetNode<Timer>("DamageCooldown");
        _damageCooldown.Start();
        _damageCooldown.Timeout += OnDamageReady;
    }

    private void OnAttackReady()
    {
        _projectileCooldown.Start();
        var projectile = ProjectilePrefab.Instantiate<Area3D>();
        projectile.BodyEntered += OnBodyEntered;
        projectile.BodyExited += OnBodyExited;
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = _gameManager.GetRandomPosAroundPlayer(ProjectileRange) + new Vector3(0, 0.1f, 0);

        var tweener = GetTree().CreateTween();
        tweener.TweenProperty(projectile.GetNode("Visual"), "scale", new Vector3(0.01f, 0.01f, 0.01f), 1).SetDelay(TotalDuration);
        tweener.Parallel().TweenCallback(Callable.From(() => projectile.SetPhysicsProcess(false))).SetDelay(TotalDuration);
        tweener.TweenCallback(Callable.From(projectile.QueueFree));
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not Enemy enemy) return;
        _enemies.Remove(enemy);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Enemy enemy) return;
        _enemies.Add(enemy);
    }

    private void OnDamageReady()
    {
        _damageCooldown.Start();
        foreach (var enemy in _enemies)
            enemy.TakeDamages(TotalDamages);
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.SpiritWaterDamages: _damagesBonus += 1; break;
            case PowerupType.SpiritWaterDuration: _durationBonus += 0.2f; break;
            case PowerupType.SpiritWaterCooldown: _cooldownBonus += 0.25f; break;
            default: break;
        }
    }
}
