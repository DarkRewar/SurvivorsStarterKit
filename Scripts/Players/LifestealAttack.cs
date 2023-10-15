using Godot;

public partial class LifestealAttack : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    [Export]
    public float Cooldown = 5;

    private float _cooldownBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;
    public float TotalCooldown => Cooldown + _cooldownBonus;

    private Player _player;
    private Timer _stealCooldown;
    private Area3D _area;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _player = GetParent<Player>();
        _stealCooldown = GetNode<Timer>("Timer");
        _stealCooldown.WaitTime = 1f / TotalCooldown;
        _stealCooldown.Start();
        _area = GetNode<Area3D>("Area3D");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        if (_stealCooldown.TimeLeft > 0) return;

        _stealCooldown.Start();
        if (!_area.HasOverlappingBodies()) return;
        foreach (var body in _area.GetOverlappingBodies())
        {
            if (body is not Enemy enemy) continue;
            enemy.TakeDamages(TotalDamages);
            _player.Heal(TotalDamages);
        }
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.LifestealDamages:
                _damagesBonus += 1;
                break;
            case PowerupType.LifestealCooldown:
                _cooldownBonus += 0.5f;
                _stealCooldown.WaitTime = 1f / TotalCooldown;
                break;
            default: break;
        }
    }
}
