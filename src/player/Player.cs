using Godot;
using System;

public class Player : Node2D
{
    private static ulong idCount = 0;

    public static event Action<Player> OnTurnEndRequested;

    public ulong Id { get; private set; }
    public bool IsBot { get; private set; }

    private bool isInitialized = false;

    private static float minimumTurnProcessTime = .1f;
    private bool isMyTurn;
    private float turnDelta;

    private bool hasChoosenStartingRegion;

    public override void _Ready()
    {
        if (!isInitialized)
        {
            GD.PushError($"Tried to create a player with id {Id} named {Name}. This is not allowed, please create player using PlayerManager API.");
        }

        TurnManager.OnWaitingForPlayer += HandleTurnWaitingForPlayer;
    }

    public override void _ExitTree()
    {
        TurnManager.OnWaitingForPlayer -= HandleTurnWaitingForPlayer;
    }

    public override void _Process(float delta)
    {
        if (isMyTurn)
        {
            turnDelta += delta;

            if (turnDelta >= minimumTurnProcessTime)
            {
                isMyTurn = false;
                turnDelta = 0;
                HandleMyTurn();
            }
        }
    }

    public void Initialize(bool isBot)
    {
        isInitialized = true;
        Id = idCount++;
        IsBot = isBot;
    }

    public void RequestTurnEnd()
    {
        OnTurnEndRequested?.Invoke(this);
    }

    private void HandleTurnWaitingForPlayer(int turn, Player previousPlayer, Player currentPlayer)
    {
        if (!IsBot) return;
        if (currentPlayer != this) return;

        isMyTurn = true;
    }

    private void HandleMyTurn()
    {
        if (!hasChoosenStartingRegion)
        {
            ChooseStartingRegion();
            RequestTurnEnd();
        }
    }

    private void ChooseStartingRegion()
    {
        if (hasChoosenStartingRegion) return;

        Region region = RegionManager.GetRandomUnoccupiedRegion();
        if (region == null) return;

        region.SetOccupier(this);
        hasChoosenStartingRegion = true;
    }
}
