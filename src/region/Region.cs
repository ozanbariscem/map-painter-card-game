using Godot;
using System;
using System.Collections.Generic;

public class Region : Node2D
{
    public static event Action<Region> OnReady;
    public static event Action<Region> OnTurnDone;
    public static event Action<Region, Player> OnRegionOccupied;
    public static event Action<Region> OnMouseEnter;
    public static event Action<Region> OnMouseExit;

    public static event Action<Region, Card> OnBattlePlanChanged;

    private static int idCount = 0;

    [Export] public int Id;
    // We can't export ulong arrays on Godot but i'm pretty sure we wont need an id bigger than 2,147,483,647
    [Export] public int[] Neighbours = { 0 };

    public byte Income { get; private set; }

    public Player Occupier { get; private set; }

    public Dictionary<Battle.BattleSide, Dictionary<ulong, Card>> BattlePlan { get; private set; }

    public Vector2 MinBounds => new Vector2(-border.Texture.GetWidth() * Scale.x / 2f, -border.Texture.GetHeight() * Scale.y / 2f);
    public Vector2 MaxBounds => new Vector2(border.Texture.GetWidth() * Scale.x / 2f, border.Texture.GetHeight() * Scale.y / 2f);

    private Sprite border;
    private Area2D area;
    private Sprite outline;
    private RegionHUD hud;

    public Region()
    {
        Id = idCount++;
        Random random = new Random();
        Income = (byte)(random.Next(2) + 1);
        Name = $"Region ({Id})";

        BattlePlan = new Dictionary<Battle.BattleSide, Dictionary<ulong, Card>>();
        BattlePlan.Add(Battle.BattleSide.Attacker, new Dictionary<ulong, Card>());
        BattlePlan.Add(Battle.BattleSide.Defender, new Dictionary<ulong, Card>());
    }

    public override void _Ready()
    {
        GetNodes();
        hud.SetRegion(this);

        area.Connect("mouse_entered", this, nameof(OnMouseEntered));
        area.Connect("mouse_exited", this, nameof(OnMouseExited));

        Card.OnDeath += HandleCardDeath;
        Card.OnRegionChanged += HandleCardRegionChanged;
        Battle.OnAttackerWin += HandleAttackerWin;
        TurnManager.OnProcessing += HandleTurnProcessing;
        OnReady?.Invoke(this);
    }

    public override void _ExitTree()
    {
        area.Disconnect("mouse_entered", this, nameof(OnMouseEntered));
        area.Disconnect("mouse_exited", this, nameof(OnMouseExited));

        Card.OnDeath -= HandleCardDeath;
        Card.OnRegionChanged -= HandleCardRegionChanged;
        Battle.OnAttackerWin -= HandleAttackerWin;
        TurnManager.OnProcessing -= HandleTurnProcessing;
    }

    public void SetNeighbours(int[] neighbours)
    {
        Neighbours = neighbours;
    }

    public void SetOccupier(Player occupier)
    {
        Occupier = occupier;
        border.Modulate = Occupier.Color;
        OnRegionOccupied?.Invoke(this, occupier);
    }

    private void HandleAttackerWin(Region region, Player defender, Player attacker)
    {
        if (region != this) return;

        if (BattlePlan[Battle.BattleSide.Defender].Count > 0)
        {
            GD.PushError($"Attackers won in this region but there are still defenders live?");
            return;
        }
        SetOccupier(attacker);
        BattlePlan[Battle.BattleSide.Defender].Clear();
        foreach (var card in BattlePlan[Battle.BattleSide.Attacker].Values)
        {
            BattlePlan[Battle.BattleSide.Defender].Add(card.Id, card);
        }
        BattlePlan[Battle.BattleSide.Attacker].Clear();
    }

    private void HandleTurnProcessing(int turn, Player currentPlayer, Player nextPlayer)
    {
        if (Occupier != null && Occupier == currentPlayer) Occupier.GiveGold(Income);
        if (BattlePlan[Battle.BattleSide.Attacker].Count > 0)
        {
            BattleManager.Instance.CreateBattle(
                this, 
                BattlePlan[Battle.BattleSide.Attacker], 
                BattlePlan[Battle.BattleSide.Defender]);
        }
        OnTurnDone?.Invoke(this);
    }

    private void HandleCardRegionChanged(Card card, Region oldRegion, Region newRegion)
    {
        if (oldRegion == this)
        {
            BattlePlan[Battle.BattleSide.Attacker].Remove(card.Id);
            BattlePlan[Battle.BattleSide.Defender].Remove(card.Id);
            OnBattlePlanChanged?.Invoke(this, card);
        }
        if (newRegion == this)
        {
            AddCard(card);
        }
    }

    private void HandleCardDeath(Card card)
    {
        if (card.Region == this)
        {
            if (card.Type == CardType.Military)
            {
                BattlePlan[Battle.BattleSide.Attacker].Remove(card.Id);
                BattlePlan[Battle.BattleSide.Defender].Remove(card.Id);
            }
        }
    }

    private void AddCard(Card card)
    {
        if (card.Type == CardType.Military)
        {
            if (Occupier == card.Holder || Occupier == null)
            {
                if (BattlePlan[Battle.BattleSide.Defender].Count < Battle.BATTLE_WIDTH)
                {
                    BattlePlan[Battle.BattleSide.Defender].Add(card.Id, card);
                    if (Occupier == null) SetOccupier(card.Holder);
                }
            }
            else
            {
                if (BattlePlan[Battle.BattleSide.Defender].Count == 0)
                {
                    BattlePlan[Battle.BattleSide.Defender].Add(card.Id, card);
                    SetOccupier(card.Holder);
                } else
                {
                    if (BattlePlan[Battle.BattleSide.Attacker].Count < Battle.BATTLE_WIDTH)
                    {
                        BattlePlan[Battle.BattleSide.Attacker].Add(card.Id, card);
                    }
                }
            }
        }
        OnBattlePlanChanged?.Invoke(this, card);
    }

    private void GetNodes()
    {
        outline = GetNode("visuals/outline") as Sprite;
        area = GetNode("area") as Area2D;
        hud = GetNode("hud") as RegionHUD;
        border = GetNode("visuals/border") as Sprite;
    }

    public static bool RegionHasEmptySlotFor(Region region, Card card)
    {
        if (card.Type == CardType.Military)
        {
            if (region.Occupier == card.Holder || region.Occupier == null)
            {
                return (region.BattlePlan[Battle.BattleSide.Defender].Count < Battle.BATTLE_WIDTH);
            }
            else
            {
                return (region.BattlePlan[Battle.BattleSide.Attacker].Count < Battle.BATTLE_WIDTH);
            }
        }
        return true;
    }

    public void Highlight(bool value)
    {
        outline.Visible = value;
        ZIndex = value ? 1 : 0;
    }

    private void OnMouseEntered()
    {
        Highlight(true);
        OnMouseEnter?.Invoke(this);
    }

    private void OnMouseExited()
    {
        Highlight(false);
        OnMouseExit?.Invoke(this);
    }
}
