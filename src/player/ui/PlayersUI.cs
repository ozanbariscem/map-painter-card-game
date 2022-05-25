using Godot;
using System;
using System.Collections.Generic;

public class PlayersUI : Control
{
    private VBoxContainer verticalBox;
    private Dictionary<ulong, PlayerElement> playerElements;

    public override void _Ready()
    {
        GetElements();
        playerElements = new Dictionary<ulong, PlayerElement>();

        PlayerManager.OnPlayerCreated += HandlePlayerCreated;
    }

    public override void _ExitTree()
    {
        PlayerManager.OnPlayerCreated -= HandlePlayerCreated;
    }

    private void HandlePlayerCreated(object sender, Player player)
    {
        if (playerElements.ContainsKey(player.Id))
        {
            GD.PushError($"" +
                $"PlayersUI has already created a player element for player with id {player.Id}. " +
                $"Duplicates are not allowed, returning!");
            return;
        }

        PackedScene scene = GD.Load("player/ui/player_element.tscn") as PackedScene;
        PlayerElement element = scene.Instance() as PlayerElement;
        element.Initialize();
        element.SetPlayer(player);
        verticalBox.AddChild(element);

        playerElements.Add(player.Id, element);
    }

    private void GetElements()
    {
        verticalBox = GetNode("List/Content") as VBoxContainer;
    }
}
