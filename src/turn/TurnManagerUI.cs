using Godot;
using System;

public class TurnManagerUI : CanvasLayer
{
    private Label turnLabel;
    private Button endTurnButton;

    public override void _Ready()
    {
        turnLabel = GetNode("Control/Label") as Label;
        endTurnButton = GetNode("Control/Button") as Button;

        TurnManager.OnWaitingForPlayer += HandleWaitingForPlayer;
        endTurnButton.Connect("pressed", this, nameof(HandleEndTurnButtonPressed));
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        TurnManager.OnWaitingForPlayer -= HandleWaitingForPlayer;
        endTurnButton.Disconnect("pressed", this, nameof(HandleEndTurnButtonPressed));
    }

    private void HandleEndTurnButtonPressed()
    {
        TurnManager.Instance.EndTurn();   
    }

    private void HandleWaitingForPlayer(int turn, Player previousPlayer, Player currentPlayer)
    {
        turnLabel.Text = $"Turn: {turn}";
        endTurnButton.Visible = !currentPlayer.IsBot;
    }
}
