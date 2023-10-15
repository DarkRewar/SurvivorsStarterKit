using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class FloatingSphereAttack : Node3D, IUpgradable
{
    [Export]
    public uint InitialSpheres = 1;

    [Export]
    public uint Damages = 3;

    private uint _damagesBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    [Export]
    public float RotationSpeed { get; private set; } = 1;

    [Export]
    public float Duration { get; private set; } = 3;

    [Export]
    public float SphereDistance { get; private set; } = 1.4f;

    [Export]
    private PackedScene _spherePrefab;
    private List<Area3D> _spheres = new();
    private Timer _timer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        foreach (int i in Enumerable.Range(0, (int)InitialSpheres))
            AddSphere();

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = Duration;
        _timer.Timeout += OnAttackEnd;
        _timer.Start();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not Enemy enemy) return;
        enemy.TakeDamages(TotalDamages);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        RotateY((float)delta * RotationSpeed);

        //if (_rb.GetContactCount() == 0) return;
        //foreach (var contact in _rb.GetCollidingBodies())
        //{
        //    if (contact is not Enemy enemy) continue;
        //    enemy.TakeDamages(Damages);
        //}
    }

    private void AddSphere()
    {
        Area3D area = _spherePrefab.Instantiate<Area3D>();
        area.BodyEntered += OnBodyEntered;
        AddChild(area);
        _spheres.Add(area);

        RepositionSpheres();
    }

    private void RepositionSpheres()
    {
        float axis;
        Vector3 basePosition = new(SphereDistance, 0.3f, 0);
        for (int i = 0; i < _spheres.Count; i++)
        {
            axis = Mathf.Lerp(0, 360, (float)i / _spheres.Count);
            Basis basis = new Basis(new Vector3(0, 1, 0), Mathf.DegToRad(axis));
            _spheres[i].Position = basis * basePosition;
        }
    }

    private async void OnAttackEnd()
    {
        HideSpheres();
        await Task.Delay(2000);
        _timer.Start();
        ShowSpheres();
    }

    private void ShowSpheres()
    {
        foreach (var area in _spheres)
        {
            area.GetNode<GpuParticles3D>("Particles").Restart();
            area.GetNode<GpuParticles3D>("Particles").Emitting = true;
            area.SetPhysicsProcess(true);
            area.Show();
        }
    }

    private void HideSpheres()
    {
        foreach (var area in _spheres)
        {
            area.GetNode<GpuParticles3D>("Particles").Emitting = false;
            area.SetPhysicsProcess(false);
            area.Hide();
        }
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.FloatingSphereCount:
                AddSphere();
                break;
            case PowerupType.FloatingSphereDamages:
                _damagesBonus += 1;
                break;
            default: break;

        }
    }
}
