using Godot;
using System;

public partial class Player : CharacterBody3D
{
    public const uint MaxLifepoints = 200;

    [Export]
    public float Speed { get; private set; } = 5.0f;

    [Export]
    public float JumpVelocity { get; private set; } = 4.5f;

    [Export]
    private uint _lifepoints = MaxLifepoints;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private GameManager _gameManager;
    private Timer _attackTimer;
    private Node3D _visual;
    private ProgressBar _playerLifebar;
    private AnimationTree _animationTree;

    public override void _Ready()
    {
        base._Ready();

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.Player = this;
        _playerLifebar = GetTree().CurrentScene.GetNode<ProgressBar>("HUD/PlayerLifeBar");
        _playerLifebar.MaxValue = MaxLifepoints;
        _playerLifebar.Value = _lifepoints;

        _attackTimer = GetNode<Timer>("AttackCooldown");
        _visual = GetNode<Node3D>("Visual");
        _animationTree = GetNode<AnimationTree>("AnimationTree");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _playerLifebar.Value = _lifepoints;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        //if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        //    velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        velocity.Y = 0;
        _animationTree.Set("parameters/walking/blend_amount", velocity.LimitLength(1).Length());
        if (Mathf.IsZeroApprox(velocity.Length())) return;

        _visual.LookAt(Position + 10 * velocity, Vector3.Up, true);
        //var angle = 0;
        //var transform = _visual.Transform;
        //transform.Basis = new(Vector3.Up, angle);
        //_visual.Transform = transform;
    }

    public void OnAttackTimeOut()
    {
        _attackTimer.Start();

        var nearestEnemy = _gameManager.GetNearestEnemy();
        if (nearestEnemy == null) return;

        //nearestEnemy.TakeDamages();
    }

    public void TakeDamages(uint damages)
    {
        _lifepoints -= Math.Clamp(damages, 0, _lifepoints);
    }

    internal void Heal(uint damages)
    {
        _lifepoints = Math.Clamp(_lifepoints + damages, 0, MaxLifepoints);
    }
}
