using Godot;
using System;

public class PlayerElement : Control
{
    public Label NameLabel { get; private set; }
    public Label InfoLabel { get; private set; }

    private Player player;

    public void Initialize()
    {
        NameLabel = GetNode("Name") as Label;
        InfoLabel = GetNode("Info") as Label;
    }

    public void SetPlayer(Player player)
    {
        if (player != null)
        {
            Unsubscribe();
        }

        this.player = player;
        UpdateMenu();
        Subscribe();
    }

    private void UpdateMenu()
    {
        NameLabel.Text = $"{player.Name} ({player.Id})";
        InfoLabel.Text = $"Player information goes here.";
    }

    private void Subscribe()
    {

    }

    private void Unsubscribe()
    {
        if (player == null) return;
        
    }
}
