using Godot;
using System;
using System.Collections.Generic;

public class TurnManager : Node2D
{
    public static TurnManager Instance { get; private set; }

    public enum TurnState { WaitingForPlayer, Processing }

    public static event Action<int, Player, Player> OnProcessing; // int: turn, player: current player, player: next player
    public static event Action<int, Player, Player> OnWaitingForPlayer; // int: turn, player: ex player, player: current player

    [Export] public bool Debug = false;

    public TurnState State { get; private set; }
    public int Turn { get; private set; } = 0;

    private List<Player> players;
    
    private int regionCount = 0;
    private int doneRegionCount = 0;

    private bool processTurn;
    private bool regionsAreDone;
    private bool battleManagerIsDone;

    public TurnManager()
    {
        if (Instance != null) return;
        Instance = this;
    }

    public override void _Ready()
    {
        Region.OnReady += HandleRegionReady;
        Region.OnTurnDone += HandleRegionTurnDone;
        Player.OnTurnEndRequested += EndTurn;
        PlayerManager.OnPlayerCreated += HandlePlayerCreated;
        BattleManager.OnTurnDone += HandleBattleManagerTurnDone;
    }

    public override void _ExitTree()
    {
        Region.OnReady -= HandleRegionReady;
        Region.OnTurnDone -= HandleRegionTurnDone;
        Player.OnTurnEndRequested -= EndTurn;
        PlayerManager.OnPlayerCreated -= HandlePlayerCreated;
        BattleManager.OnTurnDone -= HandleBattleManagerTurnDone;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.IsPressed() && keyEvent.Scancode == (uint)KeyList.Space)
            {
                processTurn = !processTurn;
                if (processTurn)
                {
                    GD.PushError($"Continued Turn Manager");
                    if (!CheckIfProcessingIsDone())
                    {
                        GD.PushError($"Manually ended turn {Turn}. This could be dangerous since processing was NOT done yet.");
                        EndTurn(players[TurnToPlayerIndex(Turn)]);
                    }
                } else
                {
                    GD.PushError($"Stopped Turn Manager");
                }
            }
        }
    }

    public void EndTurn()
    {
        Player player = players[TurnToPlayerIndex(Turn)];
        if (player.IsBot) return; // Can't make that choice

        EndTurn(player);
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

        if (Debug) GD.Print($"Turn {Turn} processing!");
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
            regionsAreDone = true;
            if (Debug) GD.Print($"Regions are done for turn {Turn}.");
            CheckIfProcessingIsDone();
        }
    }

    private void HandleBattleManagerTurnDone()
    {
        battleManagerIsDone = true;
        if (Debug) GD.Print($"Battles are done for turn {Turn}.");
        CheckIfProcessingIsDone();
    }

    private bool CheckIfProcessingIsDone()
    {
        if (regionsAreDone && battleManagerIsDone && processTurn)
        {
            regionsAreDone = false;
            battleManagerIsDone = false;
            doneRegionCount = 0;

            Turn++;
            if (Debug) GD.Print($"Turn {Turn} started!");
            OnWaitingForPlayer?.Invoke(Turn, players[TurnToPlayerIndex(Turn - 1)], players[TurnToPlayerIndex(Turn)]);
            return true;
        }
        return false;
    }

    private int TurnToPlayerIndex(int turn)
    {
        return turn % players.Count;
    }
}
