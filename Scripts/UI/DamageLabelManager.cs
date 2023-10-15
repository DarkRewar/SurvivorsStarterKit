using Godot;
using System.Collections.Generic;

public partial class DamageLabelManager : Control
{
    public const uint InitialQueue = 50;

    private readonly Queue<Label> _queue = new();

    public readonly List<Label> _displayedLabels = new();

    private Camera3D _camera;
    private GameManager _gameManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _camera = GetNode<Camera3D>("../Player/Camera3D");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.OnEnemyHit += OnEnemyHit;

        for (uint i = 0; i < InitialQueue; ++i)
        {
            var label = new Label() { ProcessMode = ProcessModeEnum.Always };
            _queue.Enqueue(label);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public Label GetLabel()
    {
        var label = _queue.Count == 0 ? new Label() { ProcessMode = ProcessModeEnum.Always } : _queue.Dequeue();
        label.LabelSettings = new LabelSettings()
        {
            FontColor = new Color(1, 0, 0),
            FontSize = 30,
            OutlineSize = 4,
            OutlineColor = new Color(0, 0, 0),
        };
        label.Modulate = new Color(1, 1, 1, 1);
        _displayedLabels.Add(label);
        AddChild(label);
        return label;
    }

    public void ReturnToPool(Label label)
    {
        _queue.Enqueue(label);
        _displayedLabels.Remove(label);
        RemoveChild(label);
    }

    public void OnEnemyHit(Enemy enemy, int damages)
    {
        var label = GetLabel();
        label.Text = damages.ToString();

        Vector2 labelPos = _camera.UnprojectPosition(enemy.GlobalPosition);
        labelPos.X -= label.Size.X / 2;
        labelPos.Y -= 50;
        label.Position = labelPos;
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", labelPos.Y - 50, 0.75f);
        tween.Parallel().TweenProperty(label, "modulate", new Color(0, 0, 0, 0), 0.5f).SetDelay(0.25f);
        tween.TweenCallback(Callable.From(() => ReturnToPool(label)));
    }
}
