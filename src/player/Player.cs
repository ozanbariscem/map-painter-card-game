using Godot;
using System;

public class Player : Node2D
{
    private static ulong idCount = 0;

    public ulong Id { get; private set; }
    public bool IsBot { get; private set; }

    private bool isInitialized = false;
    
    public override void _Ready()
    {
        if (!isInitialized)
        {
            GD.PushError($"Tried to create a player with id {Id} named {Name}. This is not allowed, please create player using PlayerManager API.");
        }
    }

    public void Initialize(bool isBot)
    {
        isInitialized = true;
        Id = idCount++;
        IsBot = isBot;
    }
}
