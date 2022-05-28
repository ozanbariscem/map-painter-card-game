using Godot;
using System;
using System.Collections.Generic;

public class TurnManager : Node2D
{
    public enum TurnState { WaitingForPlayer, Processing }

    public static event Action<int, Player, Player> OnProcessing;
    public static event Action<int, Player, Player> OnWaitingForPlayer;

    public TurnState State { get; private set; }
    public int Turn = 0;

    private List<Player> players;
    
    private int regionCount = 0;
    private int doneRegionCount = 0;
    
    public override void _Ready()
    {
        Region.OnReady += HandleRegionReady;
        Region.OnTurnDone += HandleRegionTurnDone;
        Player.OnTurnEndRequested += EndTurn;
        PlayerManager.OnPlayerCreated += HandlePlayerCreated;
    }

    public override void _ExitTree()
    {
        Region.OnReady -= HandleRegionReady;
        Region.OnTurnDone -= HandleRegionTurnDone;
        Player.OnTurnEndRequested -= EndTurn;
        PlayerManager.OnPlayerCreated -= HandlePlayerCreated;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.IsPressed() && keyEvent.Scancode == (uint)KeyList.Space)
            {
                GD.Print($"Tried to manually end turn {Turn}!");
                EndTurn(players[TurnToPlayerIndex(Turn)]);
            }
        }
    }

    private void EndTurn(Player player)
    {
        if (players[TurnToPlayerIndex(Turn)] != player)
        {
            GD.PushError($"Tried to EndTurn on some other players turn. Turn: {players[TurnToPlayerIndex(Turn)].Name} Requested: {player.Name}.");
            return;
        }
        if (State == TurnState.Processing)
        {
            GD.PushError($"Tried to EndTurn but TurnManager is still processing.");
            return;
        }

        GD.Print($"Turn {Turn} processing!");
        OnProcessing?.Invoke(Turn, players[TurnToPlayerIndex(Turn)], players[TurnToPlayerIndex(Turn+1)]);
    }

    private void HandlePlayerCreated(object sender, Player player)
    {
        if (players == null)
            players = new List<Player>();

        players.Add(player);
    }

    private void HandleRegionReady(Region region)
    {
        regionCount += 1;
    }

    private void HandleRegionTurnDone(Region region)
    {
        doneRegionCount += 1;

        if (doneRegionCount == regionCount)
        {
            doneRegionCount = 0;

            Turn++;
            GD.Print($"Turn {Turn} started!");
            OnWaitingForPlayer?.Invoke(Turn, players[TurnToPlayerIndex(Turn-1)], players[TurnToPlayerIndex(Turn)]);
        }
    }

    private int TurnToPlayerIndex(int turn)
    {
        return turn % players.Count;
    }
}
