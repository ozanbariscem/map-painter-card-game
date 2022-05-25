using Godot;
using System;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public class Card : Node2D
{
    public enum STATE { Idle, Dragging }

    public static event Action<Card> OnClicked;

    private ulong idCount = 0;

    public ulong Id { get; private set; }
    public string Tag { get; private set; }

    public CardType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }

    public byte Attack { get; private set; }
    public byte Defence { get; private set; }

    public STATE State { get; private set; }

    public string ScriptPath => $"data/cards/{Tag}.lua";
    public MoonSharp.Interpreter.Script Script { get; private set; }

    private bool isClicked;
    private bool isInitialized;

    private float moveAwaySpeed = 5;
    private float dragSpeed = 20;
    private Vector2 dragTarget;
    private Vector2 dragOffset;

    private Sprite outline;
    private Area2D area;
    private Control hud;

    private Dictionary<ulong, Card> cardsInMyRange;

    public void Initialize(string tag)
    {
        isInitialized = true;

        Id = idCount++;
        Tag = tag;

        cardsInMyRange = new Dictionary<ulong, Card>();

        SetScript();
        GetScriptData();

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
            return;
        }

        outline = GetNode("Visuals/Outline") as Sprite;
        hud = GetNode("HUD") as Control;
        area = GetNode("Area") as Area2D;
        area.Connect("area_entered", this, nameof(HandleAreaEntered));
        area.Connect("area_exited", this, nameof(HandleAreaExited));

        hud.Connect("gui_input", this, nameof(HandleInputEvent));
        hud.Connect("mouse_entered", this, nameof(HandleMouseEntered));
        hud.Connect("mouse_exited", this, nameof(HandleMouseExited));

        if (!Script.Globals.Get("OnReady").IsNil())
            Script.Call(Script.Globals["OnReady"]);
    }

    public override void _ExitTree()
    {
        area.Disconnect("area_entered", this, nameof(HandleAreaEntered));
        area.Disconnect("area_exited", this, nameof(HandleAreaExited));

        hud.Disconnect("gui_input", this, nameof(HandleInputEvent));
        hud.Disconnect("mouse_entered", this, nameof(HandleMouseEntered));
        hud.Disconnect("mouse_exited", this, nameof(HandleMouseExited));
    }

    public override void _Process(float delta)
    {
        switch (State)
        {
            case STATE.Idle:
                foreach (var card in cardsInMyRange.Values)
                {
                    MoveAwayFromCard(card, delta);
                }
                break;
            case STATE.Dragging:
                DragToTarget(dragTarget, delta);
                break;
        }
    }

    private void DragToTarget(Vector2 target, float delta)
    {
        GlobalPosition = GlobalPosition.LinearInterpolate(target+dragOffset, dragSpeed * delta);
        if (!isClicked && GlobalPosition.DistanceTo(target+dragOffset) < 5f)
        {
            State = STATE.Idle;
        }
    }

    private void MoveAwayFromCard(Card card, float delta)
    {
        Vector2 distance = GlobalPosition - card.GlobalPosition;
        GlobalPosition = GlobalPosition.LinearInterpolate(GlobalPosition + distance, moveAwaySpeed * delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (State != STATE.Dragging || !isClicked) return;

        if (@event is InputEventMouseMotion motionEvent)
        {
            dragTarget = GetGlobalMousePosition();
        }

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (!(mouseEvent.IsPressed() && mouseEvent.ButtonIndex == 1))
            {
                dragTarget = GetGlobalMousePosition();
                isClicked = false;
                return;
            }
        }
    }

    private void HandleAreaEntered(Area2D area)
    {
        Card card = area.GetParent() as Card;
        if (card == null) return;

        if (!cardsInMyRange.ContainsKey(card.Id))
        {
            cardsInMyRange.Add(card.Id, card);
        }
    }

    private void HandleAreaExited(Area2D area)
    {
        Card card = area.GetParent() as Card;
        if (card == null) return;

        if (cardsInMyRange.ContainsKey(card.Id))
        {
            cardsInMyRange.Remove(card.Id);
        }
    }

    private void HandleInputEvent(InputEvent inputEvent)
    {
        if (!(inputEvent is InputEventMouseButton mouseEvent)) return;
        if (!(mouseEvent.IsPressed() && mouseEvent.ButtonIndex == 1)) return;
        GetTree().SetInputAsHandled();

        dragOffset = GlobalPosition - GetGlobalMousePosition();
        dragTarget = GetGlobalMousePosition();
        State = STATE.Dragging;
        isClicked = true;
        OnClicked?.Invoke(this);
    }

    private void HandleMouseEntered()
    {
        outline.Visible = true;
    }

    private void HandleMouseExited()
    {
        outline.Visible = false;
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

        Attack = (byte)Script.Globals.Get("attack").Number;
        Defence = (byte)Script.Globals.Get("defence").Number;
    }
}
