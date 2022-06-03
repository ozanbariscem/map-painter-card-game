using Godot;
using System;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public class Card : Node2D
{
    public enum STATE { Idle, Dragging, MovingToTarget, ReadyToAttack, Attack, Attacked, Retrieve, Defending, Dying }

    public static event Action<Card> OnDeath;
    public static event Action<Card> OnCreated;
    public static event Action<Card> OnClicked;
    public static event Action<Card, Region, Region> OnRegionChanged;

    private static ulong idCount = 0;

    public ulong Id { get; private set; }
    public string Tag { get; private set; }

    public CardType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }

    public ushort Price { get; private set; }
    public ushort Upkeep { get; private set; }

    public byte Attack { get; private set; }
    public byte Defence { get; private set; }

    public float Range { get; private set; }

    public Region Region { get; private set; }

    public STATE State { get; private set; }

    public Player Holder { get; private set; }

    public string ScriptPath => $"data/cards/{Tag}.lua";
    public MoonSharp.Interpreter.Script Script { get; private set; }

    private bool isClicked;
    private bool isInitialized;

    private float moveAwaySpeed = 5;
    private float dragSpeed = 20;
    private Vector2 dragTarget;
    private Vector2 dragOffset;

    private Sprite outline;
    private Sprite border;
    private Sprite background;
    private Area2D area;
    private Control hud;

    private Label titleLabel;
    private Label attackLabel;
    private Label defenceLabel;

    private Dictionary<ulong, Card> cardsInMyRange;
    private Dictionary<int, Region> hoveringRegions;
    private Region closestRegion;

    private bool shake;
    private static float shakeTime = .2f;
    private float shakeDelta;

    public Card Target { get; private set; }
    private float attackSpeed = 1500;

    public void Initialize(string tag, ulong holderId, int regionId)
    {
        Region region = RegionManager.GetRegion(regionId);
        if (region == null)
        {
            GD.PushError($"Can't initialize a card without proper region.");
            return;
        }
        Holder = PlayerManager.GetPlayer(holderId);
        if (Holder == null)
        {
            GD.PushError($"Can't initialize a card without proper owner.");
            return;
        }

        isInitialized = true;

        Id = idCount++;
        Tag = tag;

        cardsInMyRange = new Dictionary<ulong, Card>();
        hoveringRegions = new Dictionary<int, Region>();

        SetRegion(region);
        SetScript();
        GetScriptData();
        GetNodes();
        SetNodeValues();

        OnCreated?.Invoke(this);
        if (!Script.Globals.Get("OnInitialized").IsNil())
            Script.Call(Script.Globals["OnInitialized"]);
    }

    public override void _Ready()
    {
        if (!isInitialized)
        {
            GD.PushError($"" +
                $"A card with id: {Id} and tag {Tag} has been pushed to scene without getting initialized. " +
                $"This is expected if a card has been created using new Card() method. " +
                $"Please create cards using CardManager.Create().");
            QueueFree();
            return;
        }

        Range = GlobalPosition.DistanceTo(GlobalPosition + new Vector2(border.Texture.GetWidth() / 2f, border.Texture.GetHeight() / 2f));
        Subscribe();

        if (!Script.Globals.Get("OnReady").IsNil())
            Script.Call(Script.Globals["OnReady"]);
    }

    public override void _ExitTree()
    {
        Unsubscribe();
    }

    public override void _Process(float delta)
    {
        if (shake)
        {
            shakeDelta += delta;
            if (shakeDelta > shakeTime)
            {
                shake = false;
                shakeDelta = 0;
                GlobalRotationDegrees = 0;
            }
            else
            {
                Shake(delta);
            }
        }

        switch (State)
        {
            case STATE.Idle:
                foreach (var card in cardsInMyRange.Values)
                {
                    // Ignore the poor thing
                    if (card.State == STATE.Dying) continue;
                    MoveAwayFromCard(card, delta);
                }
                break;
            case STATE.Dragging:
                DragToTarget(dragTarget, delta);
                break;
            case STATE.MovingToTarget:
                MoveToTarget(delta);
                break;
            case STATE.ReadyToAttack:
                AttackTarget(delta);
                break;
            case STATE.Retrieve:
                RetrieveFromTarget(delta);
                break;
            case STATE.Dying:
                Scale -= Vector2.One * delta;
                if (Scale.LengthSquared() < .05f)
                    QueueFree();
                break;
        }
        if (State != STATE.Retrieve && State != STATE.Dragging) ClampPosition();
    }

    private void Shake(float delta)
    {
        Random random = new Random();
        float newAngle = GlobalRotationDegrees + random.Next(-20, 20);
        GlobalRotationDegrees = Mathf.Lerp(GlobalRotationDegrees, newAngle, 10 * delta);
    }

    private void DragToTarget(Vector2 target, float delta)
    {
        GlobalPosition = GlobalPosition.LinearInterpolate(target+dragOffset, dragSpeed * delta);
        if (!isClicked && GlobalPosition.DistanceTo(target+dragOffset) < 5f)
        {
            SetState(STATE.Idle);
        }
    }

    private void MoveAwayFromCard(Card card, float delta)
    {
        Vector2 distance = GlobalPosition - card.GlobalPosition;
        GlobalPosition = GlobalPosition.LinearInterpolate(GlobalPosition + distance, moveAwaySpeed * delta);
    }

    private void MoveToOwnRegion(float delta)
    {
        Vector2 distance = GlobalPosition - Region.GlobalPosition;
        Vector2 position = GlobalPosition.LinearInterpolate(GlobalPosition + distance, moveAwaySpeed * delta);
        position = new Vector2(
            Mathf.Clamp(
                position.x,
                Region.GlobalPosition.x + Region.MinBounds.x + (outline.Texture.GetWidth() * Scale.x / 2f),
                Region.GlobalPosition.x + Region.MaxBounds.x - (outline.Texture.GetWidth() * Scale.x / 2f)),
            Mathf.Clamp(
                position.y,
                Region.GlobalPosition.y + Region.MinBounds.y + (outline.Texture.GetHeight() * Scale.y / 2f),
                Region.GlobalPosition.y + Region.MaxBounds.y - (outline.Texture.GetHeight() * Scale.y / 2f)));
        GlobalPosition = position;
    }

    private void ClampPosition()
    {
        Vector2 position = GlobalPosition;
        position = new Vector2(
            Mathf.Clamp(
                position.x,
                Region.GlobalPosition.x + Region.MinBounds.x + (outline.Texture.GetWidth() * Scale.x / 2f),
                Region.GlobalPosition.x + Region.MaxBounds.x - (outline.Texture.GetWidth() * Scale.x / 2f)),
            Mathf.Clamp(
                position.y,
                Region.GlobalPosition.y + Region.MinBounds.y + (outline.Texture.GetHeight() * Scale.y / 2f),
                Region.GlobalPosition.y + Region.MaxBounds.y - (outline.Texture.GetHeight() * Scale.y / 2f)));
        GlobalPosition = position;
    }

    public override void _Input(InputEvent @event)
    {
        if (State != STATE.Dragging || !isClicked) return;

        if (@event is InputEventMouseMotion motionEvent)
        {
            dragTarget = GetGlobalMousePosition();

            if (hoveringRegions.Count > 0)
            {
                HandleHoveringOverRegions();
            }
        }

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (!(mouseEvent.IsPressed() && mouseEvent.ButtonIndex == 1))
            {
                dragTarget = GetGlobalMousePosition();
                isClicked = false;
                PathManager.HidePath();
                SetRegion(closestRegion);
                return;
            }
        }
    }

    public void SetTarget(Card card)
    {
        Target = card;
        if (card == null)
        {
            ZIndex = 10;
            SetState(STATE.Idle);
            return;
        }

        ZIndex = 11;
        if (!IsCloseToTarget())
            SetState(STATE.MovingToTarget);
        else
            SetState(STATE.ReadyToAttack);
    }

    private void MoveToTarget(float delta)
    {
        GlobalPosition = GlobalPosition.MoveToward(Target.GlobalPosition, attackSpeed * delta);
        if (IsCloseToTarget())
        {
            SetState(STATE.ReadyToAttack);
        }
    }

    private bool IsCloseToTarget()
    {
        if (GlobalPosition.DistanceTo(Target.GlobalPosition) < Range + Target.Range) return true;
        return false;
    }

    private void AttackTarget(float delta)
    {
        Vector2 distance = GlobalPosition - Target.GlobalPosition;
        distance = distance.Normalized().Round();
        GlobalPosition = GlobalPosition.MoveToward(Target.GlobalPosition + distance, attackSpeed * delta);
        if (GlobalPosition.DistanceTo(Target.GlobalPosition) < 5)
        {
            SetState(STATE.Attacked);
        }
    }

    private void RetrieveFromTarget(float delta)
    {
        Vector2 distance = GlobalPosition - Target.GlobalPosition;
        GlobalPosition = GlobalPosition.MoveToward(GlobalPosition + distance, attackSpeed * delta);
        if (GlobalPosition.DistanceTo(Target.GlobalPosition) >= Range / 2f)
        {
            SetState(STATE.MovingToTarget);
        }
    }

    public bool TakeDamage(Card from)
    {
        shake = true;
        int defence = Defence - from.Attack;

        if (from.Attack > Attack)
            MoveAwayFromCard(from, from.Attack * 2);

        Defence = defence > 0 ? (byte)defence : (byte)0;
        UpdateStats();

        if (Defence <= 0)
        {
            Die();
            return true;
        }
        return false;
    }

    public void SetState(STATE state)
    {
        if (State == STATE.Dying)
        {
            GD.PushError($"Tried to change state but i'm dead.");
            return;
        }

        State = state;
    }

    private void Die()
    {
        ZIndex = 12;
        SetState(STATE.Dying);
        OnDeath?.Invoke(this);
    }

    private void HandleCameraStateChange()
    {
        if (State != STATE.Dragging || !isClicked) return;
        dragTarget = GetGlobalMousePosition();
    }

    private void HandleAreaEntered(Area2D area)
    {
        Node parent = area.GetParent();
        if (parent is Card card) HandleAreaEnteredCard(card);
        if (parent is Region region) HandleAreaEnteredRegion(region);
    }

    private void HandleAreaExited(Area2D area)
    {
        Node parent = area.GetParent();
        if (parent is Card card) HandleAreaExitedCard(card);
        if (parent is Region region) HandleAreaExitedRegion(region);
    }

    private void HandleAreaEnteredCard(Card card)
    {
        if (!cardsInMyRange.ContainsKey(card.Id))
        {
            cardsInMyRange.Add(card.Id, card);
        }
    }

    private void HandleAreaExitedCard(Card card)
    {
        if (cardsInMyRange.ContainsKey(card.Id))
        {
            cardsInMyRange.Remove(card.Id);
        }
    }

    private void HandleAreaEnteredRegion(Region region)
    {
        if (!hoveringRegions.ContainsKey(region.Id))
        {
            hoveringRegions.Add(region.Id, region);
        }
    }

    private void HandleAreaExitedRegion(Region region)
    {
        if (hoveringRegions.ContainsKey(region.Id))
        {
            hoveringRegions.Remove(region.Id);
        }
    }

    private void HandleInputEvent(InputEvent inputEvent)
    {
        if (!(inputEvent is InputEventMouseButton mouseEvent)) return;
        if (!(mouseEvent.IsPressed() && mouseEvent.ButtonIndex == 1)) return;
        GetTree().SetInputAsHandled();

        dragOffset = GlobalPosition - GetGlobalMousePosition();
        dragTarget = GetGlobalMousePosition();
        SetState(STATE.Dragging);
        isClicked = true;
        OnClicked?.Invoke(this);
    }

    private void HandleMouseEntered()
    {
        outline.Visible = true;
        ZIndex = 11;
    }

    private void HandleMouseExited()
    {
        outline.Visible = false;
        ZIndex = 10;
    }

    private void HandleHoveringOverRegions()
    {
        if (closestRegion != null) closestRegion.Highlight(false);

        float closestDistance = float.MaxValue;
        foreach (var key in hoveringRegions.Keys)
        {
            if (closestRegion == null)
            {
                closestRegion = hoveringRegions[key];
            } else
            {
                float distance = GlobalPosition.DistanceSquaredTo(hoveringRegions[key].GlobalPosition);
                if (distance < closestDistance)
                {
                    closestRegion = hoveringRegions[key];
                    closestDistance = distance;
                }
            }
        }
        if (closestRegion == null) return;

        closestRegion.Highlight(true);
        PathManager.DrawPath(GlobalPosition, closestRegion.GlobalPosition);
    }

    private void HandleDeath(Card card)
    {
        // If i'm dying
        if (State == STATE.Dying) return;

        // If my target is dead
        if (card == Target)
        {
            SetTarget(null);
        }
    }

    private void HandleTurnProcessing(int turn, Player currentPlayer, Player nextPlayer)
    {
        if (Holder != null && Holder == currentPlayer) Holder.TakeGold(Upkeep);
    }

    public bool SetRegion(Region region)
    {
        if (!CanMoveTo(this, region))
        {
            return false;
        }
        Region oldRegion = Region;
        Region = region;
        if (oldRegion == null) GlobalPosition = Region.GlobalPosition; // First time being assigned to a region
        OnRegionChanged?.Invoke(this, oldRegion, Region);
        return true;
    }

    private void SetScript()
    {
        Script = Utils.ContentUtils.GetScript(ScriptPath);
    }

    private void GetScriptData()
    {
        Type = (CardType)Script.Globals.Get("type").Number;
        Title = Script.Globals.Get("title").String;
        Description = Script.Globals.Get("description").String;
        
        Price = (ushort)Script.Globals.Get("price").Number;
        Upkeep = (ushort)Script.Globals.Get("upkeep").Number;

        Attack = (byte)Script.Globals.Get("attack").Number;
        Defence = (byte)Script.Globals.Get("defence").Number;
    }

    private void GetNodes()
    {
        border = GetNode("Visuals/Border") as Sprite;
        background = GetNode("Visuals/Background") as Sprite;
        outline = GetNode("Visuals/Outline") as Sprite;

        hud = GetNode("HUD") as Control;
        area = GetNode("Area") as Area2D;

        titleLabel = GetNode("HUD/Name") as Label;
        attackLabel = GetNode("HUD/Attack/Label") as Label;
        defenceLabel = GetNode("HUD/Defence/Label") as Label;
    }

    private void SetNodeValues()
    {
        border.Modulate = Holder.Color;
        background.Modulate = ColorUtils.CardColors[Type];

        titleLabel.Text = $"{Title} ({Id})";
        attackLabel.Text = $"{Attack}";
        defenceLabel.Text = $"{Defence}";
    }

    private void Subscribe()
    {
        area.Connect("area_entered", this, nameof(HandleAreaEntered));
        area.Connect("area_exited", this, nameof(HandleAreaExited));

        hud.Connect("gui_input", this, nameof(HandleInputEvent));
        hud.Connect("mouse_entered", this, nameof(HandleMouseEntered));
        hud.Connect("mouse_exited", this, nameof(HandleMouseExited));

        OnDeath += HandleDeath;
        TurnManager.OnProcessing += HandleTurnProcessing;
        CameraController.OnZoom += HandleCameraStateChange;
        CameraController.OnMove += HandleCameraStateChange;
    }

    private void Unsubscribe()
    {
        area.Disconnect("area_entered", this, nameof(HandleAreaEntered));
        area.Disconnect("area_exited", this, nameof(HandleAreaExited));

        hud.Disconnect("gui_input", this, nameof(HandleInputEvent));
        hud.Disconnect("mouse_entered", this, nameof(HandleMouseEntered));
        hud.Disconnect("mouse_exited", this, nameof(HandleMouseExited));

        OnDeath -= HandleDeath;
        TurnManager.OnProcessing -= HandleTurnProcessing;
        CameraController.OnZoom -= HandleCameraStateChange;
        CameraController.OnMove -= HandleCameraStateChange;
    }

    private void UpdateStats()
    {
        attackLabel.Text = $"{Attack}";
        defenceLabel.Text = $"{Defence}";
    }

    public static bool CanMoveTo(Card card, Region region)
    {
        if (card.Region == null) return true;

        foreach (var neighbour in card.Region.Neighbours)
        {
            if (neighbour == region.Id)
            {
                return Region.RegionHasEmptySlotFor(region, card);
            }
        }
        return false;
    }
}
