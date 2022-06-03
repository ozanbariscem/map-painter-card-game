using Godot;
using System;

public class Player : Node2D
{
    private static ulong idCount = 0;
    public static event Action<Player> OnTurnEndRequested;
    
    public static event Action<Player> OnGoldChanged;

    public ulong Id { get; private set; }
    public bool IsBot { get; private set; }
    public Color Color { get; private set; }

    public ushort Gold 
    { 
        get => gold; 
        private set
        {
            gold = value;
            OnGoldChanged?.Invoke(this);
        }
    }

    private ushort gold;

    public AIBrain AIBrain { get; private set; }

    private bool isInitialized = false;

    public override void _Ready()
    {
        if (!isInitialized)
        {
            GD.PushError($"Tried to create a player with id {Id} named {Name}. This is not allowed, please create player using PlayerManager API.");
        }
    }

    public override void _Process(float delta)
    {
        AIBrain.Process(delta);
    }

    public void Initialize(bool isBot)
    {
        isInitialized = true;
        Id = idCount++;
        IsBot = isBot;
        Color = ColorUtils.GetRandomPlayerColor();

        AIBrain = new AIBrain(this);
    }

    public void RequestTurnEnd()
    {
        OnTurnEndRequested?.Invoke(this);
    }
}
