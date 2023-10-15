using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeView : Control
{
    private PackedScene _choicePanel;

    private List<Choice> _choices;
    private List<Label> _voteLabels = new();

    public event Action<Choice> OnChoose;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _choicePanel = (PackedScene)GD.Load("res://Prefabs/UI/powerup_block.tscn");
        ProcessMode = ProcessModeEnum.Always;
        Clear();
    }

    internal void Clear()
    {
        _voteLabels.Clear();
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    internal void SetChoices(List<Choice> choices)
    {
        Clear();

        _choices = choices;
        int choiceIndex = 1;
        foreach (var choice in choices)
        {
            var panel = _choicePanel.Instantiate<Button>();
            panel.ProcessMode = ProcessModeEnum.Always;
            panel.Pressed += () =>
            {
                panel.Disabled = true;
                OnChoose?.Invoke(choice);
            };
            AddChild(panel);

            var name = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxPlayer/Name");
            name.Text = choice.Powerup.Name;
            var description = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxPlayer/Description");
            description.Text = choice.Powerup.Description;

            name = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxEnemy/Name");
            name.Text = choice.EnemyPowerup.Name;
            description = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxEnemy/Description");
            description.Text = string.Format(choice.EnemyPowerup.Description, Math.Round(choice.EnemyValue, 1));

            var vote = panel.GetNode<Label>("MarginContainer/Vote");
            vote.Text = GetText(choiceIndex++, 0);
            _voteLabels.Add(vote);
        }
    }

    internal void UpdateChoice(int choice, int votes)
    {
        if (choice < 1 || choice > 3) return;
        _voteLabels[choice - 1].Text = GetText(choice, votes);
    }

    internal void DisplayChoicePicked(int choice)
    {
        var children = GetChildren();
        for (int i = children.Count - 1; i >= 0; --i)
        {
            if (choice - 1 == i) continue;
            RemoveChild(children[i]);
        }
    }

    private string GetText(int choice, int votes) => $"Tapez {choice} dans le chat pour voter\nNombre de votes: {votes}";
}
