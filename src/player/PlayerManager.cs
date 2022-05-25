using Godot;
using System;

public class PlayerManager : Node2D
{
    public static event EventHandler<Player> OnPlayerCreated;

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
            OnPlayerCreated?.Invoke(this, player);
        }
    }
}
