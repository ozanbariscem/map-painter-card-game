using Godot;
using System;
using System.Collections.Generic;

public class PlayerManager : Node2D
{
    public static event EventHandler<Player> OnPlayerCreated;

    private static PlayerManager Instance;

    private Dictionary<ulong, Player> players;

    public PlayerManager()
    {
        if (Instance != null) return;
        Instance = this;
        players = new Dictionary<ulong, Player>();
    }

    public override void _Ready()
    {
        CreatePlayers(8);
    }

    private void CreatePlayers(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            PackedScene playerScene = GD.Load("player/player.tscn") as PackedScene;
            Node instance = playerScene.Instance();
            Player player = instance as Player;
            player.Initialize(true);
            AddChild(player);

            players.Add(player.Id, player);
            OnPlayerCreated?.Invoke(this, player);
        }
    }

    public static Player GetPlayer(ulong id)
    {
        if (Instance == null)
        {
            GD.PushError($"Can't get player because there are no PlayerManagers active in the scene.");
            return null;
        }
        if (!Instance.players.TryGetValue(id, out var player))
        {
            GD.PushError($"Can't get player because there are no players with id {id} in game.");
            return null;
        }

        return player;
    }
}
