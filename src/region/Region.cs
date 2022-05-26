using Godot;
using System;
using System.Collections.Generic;

public class Region : Node2D
{
    public static event Action<Region> OnReady;
    public static event Action<Region> OnTurnDone;
    public static event Action<Region> OnMouseEnter;
    public static event Action<Region> OnMouseExit;

    private static int idCount = 0;

    [Export] public int Id;
    // We can't export ulong arrays on Godot but i'm pretty sure we wont need an id bigger than 2,147,483,647
    [Export] public int[] Neighbours = { 0 };

    public Player Occupier { get; private set; }

    public List<Card> Attackers { get; private set; }
    public List<Card> Defenders { get; private set; }

    public Vector2 MinBounds => new Vector2(-border.Texture.GetWidth() * Scale.x / 2f, -border.Texture.GetHeight() * Scale.y / 2f);
    public Vector2 MaxBounds => new Vector2(border.Texture.GetWidth() * Scale.x / 2f, border.Texture.GetHeight() * Scale.y / 2f);

    private Sprite border;
    private Area2D area;
    private Sprite outline;
    private RegionHUD hud;

    public Region()
    {
        Id = idCount++;
        Attackers = new List<Card>();
        Defenders = new List<Card>();
    }

    public override void _Ready()
    {
        GetNodes();
        hud.SetRegion(this);

        area.Connect("mouse_entered", this, nameof(OnMouseEntered));
        area.Connect("mouse_exited", this, nameof(OnMouseExited));

        TurnManager.OnProcessing += HandleTurnProcessing;
        OnReady?.Invoke(this);
    }

    public override void _ExitTree()
    {
        area.Disconnect("mouse_entered", this, nameof(OnMouseEntered));
        area.Disconnect("mouse_exited", this, nameof(OnMouseExited));

        TurnManager.OnProcessing -= HandleTurnProcessing;
    }

    public void SetNeighbours(int[] neighbours)
    {
        Neighbours = neighbours;
    }

    private void HandleTurnProcessing(int turn, Player currentPlayer, Player nextPlayer)
    {
        // Only create on my turn
        if (Occupier == currentPlayer)
        {
            CardManager.CreateRandomCard(currentPlayer.Id, Id);
        }
        if (Attackers.Count > 0)
        {
            // Simulate battle
        }

        OnTurnDone?.Invoke(this);
    }

    private void GetNodes()
    {
        outline = GetNode("visuals/outline") as Sprite;
        area = GetNode("area") as Area2D;
        hud = GetNode("hud") as RegionHUD;
        border = GetNode("visuals/border") as Sprite;
    }

    private void OnMouseEntered()
    {
        outline.Visible = true;
        ZIndex = 1;
        OnMouseEnter?.Invoke(this);
    }

    private void OnMouseExited()
    {
        outline.Visible = false;
        ZIndex = 0;
        OnMouseExit?.Invoke(this);
    }
}
