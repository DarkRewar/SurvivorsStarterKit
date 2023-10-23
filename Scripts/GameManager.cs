using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public record Choice(Powerup Powerup, EnemyPowerup EnemyPowerup, double EnemyValue);

public partial class GameManager : Node
{
    private const float VoteTime = 30;

    public Player Player;

    private double _enemySpawnTimeLeft = 1;

    private EnemyManager _enemyManager;

    private ProgressBar _playerXpBar;
    private float _playerXp = 0;
    private int _playerLevel = 1;
    private int _maxPlayerXP = 5;

    private bool _isVotePhase = false;
    private UpgradeView _upgradeView;
    private List<Choice> _currentVotes;
    private List<Powerup> _powerups = new();
    private List<EnemyPowerup> _enemyPowerups = new();
    private Dictionary<PowerupType, int> _powerupsCount = new();
    private Dictionary<EnemyPowerupType, int> _enemyPowerupsCount = new();

    private Label _gameTimeLabel;
    public double GameTime { get; private set; } = 0;

    public event Action<Enemy, int> OnEnemyHit;

    public override void _Ready()
    {
        base._Ready();

        _enemyManager = new(this);
        _enemySpawnTimeLeft = _enemyManager.SpawnDelay;

        ProcessMode = ProcessModeEnum.Always;

        _maxPlayerXP = GetMaxXPPerLevel(1);

        _gameTimeLabel = GetNode<Label>("/root/MainScene/HUD/GameTime");
        _playerXpBar = GetNode<ProgressBar>("/root/MainScene/HUD/PlayerXPBar");
        _playerXpBar.MaxValue = _maxPlayerXP;

        _upgradeView = GetNode<UpgradeView>("/root/MainScene/HUD/UpgradeContainer");
        _upgradeView.OnChoose += OnChoose;

        LoadPowerups();
        LoadEnemyPowerups();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isVotePhase) return;

        // Debug thing
        if (Input.IsActionJustPressed("SpawnBoss"))
        {
            _enemyManager.SpawnBoss();
        }

        GameTime += delta;
        _gameTimeLabel.Text = $"{Mathf.FloorToInt(GameTime / 60):00}:{Mathf.FloorToInt(GameTime % 60):00}";

        if (Player == null) return;

        _enemySpawnTimeLeft -= delta;
        if (_enemySpawnTimeLeft > 0) return;
        _enemySpawnTimeLeft = _enemyManager.SpawnDelay;

        //int enemiesToSpawn = 15; // <-- This is a stress test value
        int enemiesToSpawn = 1;
        if (_enemyManager.Enemies.Count <= 200)
        {
            for (int i = 0; i < enemiesToSpawn; ++i)
            {
                _enemyManager.SpawnEnemy();
            }
        }
    }

    //public override void _PhysicsProcess(double delta)
    //{
    //    base._PhysicsProcess(delta);

    //    _enemyManager._PhysicsProcess(delta);
    //}

    private void LoadPowerups()
    {
        foreach (var path in PowerupPaths.PlayerPowerups)
        {
            Powerup powerup = GD.Load<Powerup>(path);
            _powerups.Add(powerup);
            _powerupsCount.Add(powerup.Type, 0);
        }
    }

    private void LoadEnemyPowerups()
    {
        foreach (var path in PowerupPaths.EnemyPowerups)
        {
            EnemyPowerup powerup = GD.Load<EnemyPowerup>(path);
            _enemyPowerups.Add(powerup);
            _enemyPowerupsCount.Add(powerup.Type, 0);
        }
    }

    internal void EnemyHit(Enemy enemy, int damages)
    {
        OnEnemyHit?.Invoke(enemy, damages);
    }

    internal void EnemyKilled(float enemyXP)
    {
        _playerXp += enemyXP;
        _playerXpBar.Value = _playerXp;
        if (_playerXp >= _maxPlayerXP)
        {
            _playerLevel++;
            _maxPlayerXP = GetMaxXPPerLevel(_playerLevel);
            _playerXpBar.MaxValue = _maxPlayerXP;

            GetTree().Paused = true;
            DisplayPowerups();
        }
    }

    private void DisplayPowerups()
    {
        _isVotePhase = true;

        List<Powerup> powerupChoices = _powerups.Where(p => _powerupsCount[p.Type] < p.MaxCumul)
            .OrderBy(_ => GD.Randf())
            .Take(3)
            .ToList();
        List<EnemyPowerup> enemyPowerupsChoices = _enemyPowerups.Where(p => _enemyPowerupsCount[p.Type] < p.MaxStack)
            .OrderBy(_ => GD.Randf())
            .Take(3)
            .ToList();

        _currentVotes = new List<Choice>{
            new(powerupChoices[0], enemyPowerupsChoices[0], _enemyManager.GetFinalValue(enemyPowerupsChoices[0])),
            new(powerupChoices[1], enemyPowerupsChoices[1], _enemyManager.GetFinalValue(enemyPowerupsChoices[1])),
            new(powerupChoices[2], enemyPowerupsChoices[2], _enemyManager.GetFinalValue(enemyPowerupsChoices[2])),
        };
        _upgradeView.SetChoices(_currentVotes);
    }

    private async void OnChoose(Choice choice)
    {
        _upgradeView.DisplayChoicePicked(_currentVotes.IndexOf(choice) + 1);

        await Task.Delay(3000);

        var upgradables = Player.GetChildren()
            .Where(child => child is IUpgradable)
            .Select(child => child as IUpgradable);
        foreach (var upgradable in upgradables) upgradable.Upgrade(choice.Powerup.Type);

        _enemyManager.Upgrade(choice.EnemyPowerup);

        _powerupsCount[choice.Powerup.Type]++;
        _enemyPowerupsCount[choice.EnemyPowerup.Type]++;

        _isVotePhase = false;

        GetTree().Paused = false;
        _upgradeView.Clear();
        _playerXp = 0;
    }

    public Vector3 GetRandomPosAroundPlayer(float range) => Player.Position + range * new Vector3(
            (float)GD.RandRange(-1f, 1f),
            0,
            (float)GD.RandRange(-1f, 1f)
            ).Normalized();

    internal Enemy GetNearestEnemy() => _enemyManager.Enemies
        .OrderBy(enemy => (Player.Position - enemy.Position).Length())
        .FirstOrDefault();

    internal int GetMaxXPPerLevel(int level) => Mathf.RoundToInt(Math.Log10(Math.Pow(level, 10) * 10) * 5);

    internal int GetMaxEnemyLifepoints(int level) => Mathf.RoundToInt(Math.Log10(level * 10));

    internal int GetMaxEnemyLifepoints() => GetMaxEnemyLifepoints(_playerLevel);
}
