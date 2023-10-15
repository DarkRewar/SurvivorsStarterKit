

using Godot;

public partial class Shooting : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 5;

    private uint _damagesBonus = 0;

    [Export]
    public float AttackSpeed = 1;

    private float _attackSpeedBonus = 0;

    public float TotalAttackSpeed => AttackSpeed + _attackSpeedBonus;

    [Export]
    public float BulletSpeed = 2;

    private float _bulletSpeedBonus = 0;

    public float TotalBulletSpeed => BulletSpeed + _bulletSpeedBonus;

    private PackedScene _bulletPrefab;

    private GameManager _gameManager;

    private Timer _timer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _bulletPrefab = (PackedScene)GD.Load("res://Prefabs/Powerups/bullet.tscn");

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = 1f / TotalAttackSpeed;
        _timer.Timeout += Shoot;
        _timer.Start();
    }

    private void Shoot()
    {
        _timer.Start();

        var nearestEnemy = _gameManager.GetNearestEnemy();
        if (nearestEnemy == null) return;

        var bullet = _bulletPrefab.Instantiate<RigidBody3D>();
        //bullet.ConstantForce = TotalBulletSpeed * (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();
        bullet.LinearVelocity = TotalBulletSpeed * (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();
        bullet.BodyEntered += (body) => OnBodyEntered(bullet, body);
        GetTree().CurrentScene.AddChild(bullet);
        bullet.GlobalPosition = GlobalPosition + new Vector3(0, 0.5f, 0);
    }

    private void OnBodyEntered(RigidBody3D bullet, Node body)
    {
        bullet.QueueFree();
        if (body is not Enemy enemy) return;
        enemy.TakeDamages(Damages + _damagesBonus);
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.ShootingDamages:
                _damagesBonus += 1;
                break;
            case PowerupType.ShootingAttackSpeed:
                _attackSpeedBonus += 0.1f;
                _timer.WaitTime = 1f / TotalAttackSpeed;
                break;
            case PowerupType.ShootingBulletSpeed:
                _bulletSpeedBonus += 0.1f;
                break;
            default: break;
        }
    }
}
