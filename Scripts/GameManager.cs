using Godot;
using GodotSurvivors.Scripts.Twitch;
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
    private Dictionary<string, int> _playerVotes = new();
    private Timer _voteTimer;
    private UpgradeView _upgradeView;
    private List<Choice> _currentVotes;
    private ProgressBar _voteTimeBar;
    private Label _voteTimeBarLabel;

    private TwitchConnection _twitchConnection;
    private List<Powerup> _powerups = new();
    private List<EnemyPowerup> _enemyPowerups = new();
    private Dictionary<PowerupType, int> _powerupsCount = new();
    private Dictionary<EnemyPowerupType, int> _enemyPowerupsCount = new();

    private TwitchUsers _users = new();

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

        _twitchConnection = GetNode<TwitchConnection>("/root/TwitchConnection");

        _gameTimeLabel = GetNode<Label>("/root/MainScene/HUD/GameTime");
        _playerXpBar = GetNode<ProgressBar>("/root/MainScene/HUD/PlayerXPBar");
        _playerXpBar.MaxValue = _maxPlayerXP;
        _voteTimeBar = GetNode<ProgressBar>("/root/MainScene/HUD/VoteTimeBar");
        _voteTimeBar.MaxValue = VoteTime;
        _voteTimeBarLabel = _voteTimeBar.GetNode<Label>("Label");
        _twitchConnection.OnMessageReceived += OnTwitchMessage;

        _upgradeView = GetNode<UpgradeView>("/root/MainScene/HUD/UpgradeContainer");
        _upgradeView.OnChoose += OnChoose;

        _voteTimer = new()
        {
            OneShot = true,
            Autostart = false,
            WaitTime = 10,
            ProcessMode = ProcessModeEnum.Always
        };
        _voteTimer.Timeout += OnVoteFinished;
        AddChild(_voteTimer);

        LoadPowerups();
        LoadEnemyPowerups();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isVotePhase)
        {
            _voteTimeBar.Value = _voteTimer.TimeLeft;
            _voteTimeBarLabel.Text = $"Temps de vote restant : {_voteTimer.TimeLeft:0}sec.";

            return;
        }

        // Debug thing
        if (Input.IsActionJustPressed("SpawnBoss"))
        {
            _enemyManager.SpawnBoss();
        }

        GameTime += delta;
        _gameTimeLabel.Text = $"{Mathf.FloorToInt(GameTime / 60):00}:{Mathf.FloorToInt(GameTime % 60):00}";

        if (Player == null) return;

        _users.Update(delta);

        _enemySpawnTimeLeft -= delta;
        if (_enemySpawnTimeLeft > 0) return;
        _enemySpawnTimeLeft = _enemyManager.SpawnDelay;

        if (_enemyManager.Enemies.Count <= 200)
        {
            for (int i = 0; i < 15; ++i)
            {
                _enemyManager.SpawnEnemy();
            }
        }

        //_enemyManager.SpawnEnemy();

        foreach (var username in _users.Users)
        {
            var enemy = _enemyManager.SpawnEnemy();
            enemy.SetName(username);
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
        //var folderName = "res://Powerups";
        //var directory = DirAccess.Open(folderName);
        //if (directory == null) return;

        //directory.ListDirBegin();
        //string fileName = directory.GetNext();
        //while (fileName != "")
        //{
        //    if (!directory.CurrentIsDir())
        //    {
        //        Powerup powerup = GD.Load<Powerup>($"{folderName}/{fileName}");
        //        _powerups.Add(powerup);
        //        _powerupsCount.Add(powerup.Type, 0);
        //    }
        //    fileName = directory.GetNext();
        //}
    }

    private void LoadEnemyPowerups()
    {
        foreach (var path in PowerupPaths.EnemyPowerups)
        {
            EnemyPowerup powerup = GD.Load<EnemyPowerup>(path);
            _enemyPowerups.Add(powerup);
            _enemyPowerupsCount.Add(powerup.Type, 0);
        }
        //var folderName = "res://Powerups/Enemy";
        //var directory = DirAccess.Open(folderName);
        //if (directory == null) return;

        //directory.ListDirBegin();
        //string fileName = directory.GetNext();
        //while (fileName != "")
        //{
        //    if (!directory.CurrentIsDir())
        //    {
        //        EnemyPowerup powerup = GD.Load<EnemyPowerup>($"{folderName}/{fileName}");
        //        _enemyPowerups.Add(powerup);
        //        _enemyPowerupsCount.Add(powerup.Type, 0);
        //    }
        //    fileName = directory.GetNext();
        //}
    }

    public void OnTwitchMessage(string username, string message)
    {
        GD.Print($"Message sent by {username}: {message}");
        _users.OnMessage(username);

        if (_isVotePhase)
        {
            if (!int.TryParse(message, out int number)) return;

            if (!_playerVotes.TryAdd(username, number))
                _playerVotes[username] = number;

            for (int i = 1; i <= 3; ++i)
            {
                _upgradeView.UpdateChoice(i, _playerVotes.Values.Count(x => x == i));
            }
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
        _playerVotes.Clear();
        _voteTimer.Start(VoteTime);
        _playerXpBar.Hide();
        _voteTimeBar.Show();

        //List<Powerup> availables = new(_powerups);
        //List<Powerup> powerupChoices = new();
        //for (int i = 0; i < 3; ++i)
        //{
        //    var randIndex = GD.RandRange(0, availables.Count - 1);
        //    powerupChoices.Add(availables[randIndex]);
        //    availables.RemoveAt(randIndex);
        //}
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

    private void OnVoteFinished()
    {
        List<(int choice, int count)> votes = new(){
            (1, _playerVotes.Values.Count(x => x == 1)),
            (2, _playerVotes.Values.Count(x => x == 2)),
            (3, _playerVotes.Values.Count(x => x == 3))
        };
        votes = votes.OrderByDescending(vote => vote.count).ToList();

        int choice = votes[0].choice;
        if (votes[1].count == votes[0].count && votes[0].count == votes[2].count)
        {
            choice = GD.RandRange(1, 3);
        }
        else if (votes[1].count == votes[0].count)
        {
            choice = votes[GD.RandRange(0, 1)].choice;
        }

        OnChoose(_currentVotes[choice - 1]);

        _isVotePhase = false;
        _voteTimer.Stop();
    }

    private async void OnChoose(Choice choice)
    {
        _voteTimer.Stop();
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
        _playerXpBar.Show();
        _voteTimeBar.Hide();

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

    //=LOG(PUISSANCE(A2;8)*10)*5
    internal int GetMaxXPPerLevel(int level) => Mathf.RoundToInt(Math.Log10(Math.Pow(level, 10) * 10) * 5);

    internal int GetMaxEnemyLifepoints(int level) => Mathf.RoundToInt(Math.Log10(level * 10));
    internal int GetMaxEnemyLifepoints() => GetMaxEnemyLifepoints(_playerLevel);
}