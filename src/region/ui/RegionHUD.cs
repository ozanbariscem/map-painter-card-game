using Godot;
using System.Linq;

public class RegionHUD : Control
{
    public enum State { Stable, Transition }
    
    public static float transitionSpeed = 5f;

    private Label nameLabel;
    private Label ownerLabel;

    private Label cardsLabel;
    private Label incomeLabel;


    private int transitionDirection;
    private State state;
    private Region region;

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        nameLabel = GetNode("Name") as Label;
        ownerLabel = GetNode("Owner") as Label;
        cardsLabel = GetNode("Cards") as Label;
        incomeLabel = GetNode("Income") as Label;

        Region.OnMouseEnter += HandleMouseEnter;
        Region.OnMouseExit += HandleMouseExit;
        Region.OnBattlePlanChanged += HandleCardsOnRegionChanged;
    }

    public override void _ExitTree()
    {
        Region.OnMouseEnter -= HandleMouseEnter;
        Region.OnMouseExit -= HandleMouseExit;
        Region.OnBattlePlanChanged -= HandleCardsOnRegionChanged;
    }

    public override void _Process(float delta)
    {
        Transition(delta);
    }

    public void SetRegion(Region region)
    {
        this.region = region;
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        nameLabel.Text = $"{region.Name}";
        ownerLabel.Text = region.Owner != null ? $"Occupied by {region.Owner.Name}" : $"Unoccupied region";

        cardsLabel.Text =
            $"{region.Id} " +
            $"D: {region.BattlePlan[Battle.BattleSide.Defender].Count} ({region.BattlePlan[Battle.BattleSide.Defender].Values.Sum(x => x.Defence)})" +
            $"A: {region.BattlePlan[Battle.BattleSide.Attacker].Count} ({region.BattlePlan[Battle.BattleSide.Defender].Values.Sum(x => x.Attack)})";
        incomeLabel.Text = $"+{region.Income} Gold";
    }

    private void Transition(float delta)
    {
        if (state == State.Stable) return;

        Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, Modulate.a + transitionDirection * transitionSpeed * delta);
        if (state == State.Transition)
        {
            if (Modulate.a <= 0 || Modulate.a >= 1)
            {
                Modulate = new Color(1, 1, 1, Modulate.a < 1 ? 0 : 1);
                state = State.Stable;
            }
        }
    }

    private void HandleCardsOnRegionChanged(Region region, Card card)
    {
        if (this.region != region) return;
        UpdateHUD();
    }

    private void HandleMouseEnter(Region region)
    {
        if (this.region != region) return;
        state = State.Transition;
        transitionDirection = 1;
    }

    private void HandleMouseExit(Region region)
    {
        if (this.region != region) return;
        state = State.Transition;
        transitionDirection = -1;
    }
}
