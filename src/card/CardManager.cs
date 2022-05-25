using Godot;
using System;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public class CardManager : Node2D
{
    public string CardsPath => $"data/cards";

    private Dictionary<string, ushort> cards; // Tag -> Amount of cards created with this tag

    public override void _Ready()
    {
        GetCards();
        CreateCard("test_card", 9);
        CreateCard("test_card", 9);
    }

    private void GetCards()
    {
        cards = new Dictionary<string, ushort>();
        foreach (var tag in Utils.ContentUtils.GetScriptNamesOnFolder(CardsPath))
        {
            cards.Add(tag, 0);
        }
    }

    private void CreateCard(string tag, int regionId)
    {
        PackedScene scene = GD.Load("card/card.tscn") as PackedScene;
        Card card = scene.Instance() as Card;
        card.Initialize(tag, regionId);
        card.Scale = new Vector2(.1f, .1f);
        AddChild(card);
        cards[tag]++;
    }
}
