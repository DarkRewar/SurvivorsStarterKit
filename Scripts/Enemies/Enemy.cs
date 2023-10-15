using Godot;

public enum EnemyClass
{
    Minion = 0,
    Warrior = 1,
    Archer = 2,
    Mage = 3,

    Boss = 100
}

public partial class Enemy : RigidBody3D
{
    public const uint MaxLifepoints = 10;

    [Export]
    public int Lifepoints { get; set; } = (int)MaxLifepoints;

    [Export]
    public uint Damages { get; set; } = 5;

    [Export]
    public float MovementSpeed = 4;

    [Export]
    public int Experience { get; private set; } = 1;

    private GameManager _gameManager;
    private GpuParticles3D _damageParticles;

    private ProgressBar _lifebar;
    private Camera3D _camera;
    private Label _username;
    private Timer _attackCooldown;

    [Signal]
    public delegate void OnEnemyHitEventHandler(Enemy enemy, int damages);

    public bool IsPlayerInAttackRange => (_gameManager.Player.GlobalPosition - GlobalPosition).Length() <= 2f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");

        _attackCooldown = GetNode<Timer>("AttackCooldown");
        _damageParticles = GetNode<GpuParticles3D>("DamageParticles");

        _camera = GetNode<Camera3D>("../Player/Camera3D");

        // var hud = GetNode<Control>("../HUD");
        // _lifebar = GD.Load<PackedScene>("res://Prefabs/UI/enemy_life_bar.tscn").Instantiate<ProgressBar>();
        // _lifebar.MaxValue = Lifepoints;
        // hud.AddChild(_lifebar);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_attackCooldown.TimeLeft <= 0) Attack();

        LookAt(_gameManager.Player.GlobalPosition, Vector3.Up, true);

        if (_lifebar != null)
        {
            Vector2 lifeBarPos = _camera.UnprojectPosition(GlobalPosition);
            lifeBarPos -= _lifebar.Size / 2;
            lifeBarPos.Y -= 50;
            _lifebar.Position = lifeBarPos;
            _lifebar.Value = Lifepoints;
        }

        if (_username == null) return;

        Vector2 usernamePos = _camera.UnprojectPosition(GlobalPosition);
        usernamePos -= _username.Size / 2;
        usernamePos.Y -= 40;
        _username.Position = usernamePos;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var playerPos = _gameManager.Player.GlobalPosition;
        var direction = (playerPos - GlobalPosition).LimitLength();

        LinearVelocity = direction * MovementSpeed;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _lifebar?.QueueFree();
        _username?.QueueFree();
    }

    private void Attack()
    {
        if (!IsPlayerInAttackRange) return;
        _attackCooldown.Start();
        _gameManager.Player.TakeDamages(Damages);
    }

    internal void SetName(string username)
    {
        _username = new() { Text = username };
        GetNode<Control>("../HUD").AddChild(_username);
    }

    internal void TakeDamages(uint damages = 1)
    {
        EmitParticles();

        EmitSignal(SignalName.OnEnemyHit, this, damages);
        Lifepoints -= (int)damages;
        if (Lifepoints > 0) return;
        QueueFree();
    }

    internal async void EmitParticles()
    {
        var particles = _damageParticles.Duplicate() as GpuParticles3D;
        AddChild(particles);
        particles.Emitting = true;
        await ToSignal(GetTree().CreateTimer(particles.Lifetime), SceneTreeTimer.SignalName.Timeout);
        particles.QueueFree();
    }
}
