using Godot;
using System;
using System.Collections.Generic;

public class BattleManager : Node2D
{
    public static event Action OnTurnDone;

    public static BattleManager Instance { get; private set; }

    public List<Battle> Battles { get; private set; }

    public BattleManager()
    {
        if (Instance != null) return;
        Instance = this;
        Battles = new List<Battle>();
    }

    private bool checkedThisTurn = true;

    public override void _Ready()
    {
        TurnManager.OnProcessing += HandleTurnProcessing;
    }

    public override void _ExitTree()
    {
        TurnManager.OnProcessing -= HandleTurnProcessing;
    }

    public override void _Process(float delta)
    {
        if (checkedThisTurn) return;

        if (Battles.Count != 0)
        {
            if (Battles[Battles.Count - 1].Simulate())
            {
                Battles.RemoveAt(Battles.Count - 1);
            }
        } else
        {
            checkedThisTurn = true;
            OnTurnDone?.Invoke();
        }
    }

    public void CreateBattle(Region region, Dictionary<ulong, Card> attackers, Dictionary<ulong, Card> defenders)
    {
        Battles.Add(new Battle(region, attackers, defenders));
    }

    private void HandleTurnProcessing(int turn, Player currentPlayer, Player nextPlayer)
    {
        checkedThisTurn = false;
    }
}
