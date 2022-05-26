using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class CardManager : Node2D
{
    private static CardManager Instance;

    public string CardsPath => $"data/cards";

    private Dictionary<string, ushort> cards; // Tag -> Amount of cards created with this tag
    private string[] tags;

    public CardManager()
    {
        if (Instance != null) return;
        Instance = this;
    }

    public override void _Ready()
    {
        GetCards();
    }

    private void GetCards()
    {
        cards = new Dictionary<string, ushort>();
        foreach (var tag in Utils.ContentUtils.GetScriptNamesOnFolder(CardsPath))
        {
            cards.Add(tag, 0);
        }
        tags = cards.Keys.ToArray();
    }

    private Card CreateCard(string tag, ulong holderId, int regionId)
    {
        PackedScene scene = GD.Load("card/card.tscn") as PackedScene;
        Card card = scene.Instance() as Card;
        card.Initialize(tag, holderId, regionId);
        card.Scale = new Vector2(.1f, .1f);
        AddChild(card);
        cards[tag]++;

        return card;
    }

    public static Card CreateRandomCard(ulong playerId, int regionId)
    {
        if (Instance == null)
        {
            GD.PushError($"Can't create card because there are no CardManagers active in the scene.");
            return null;
        }
        Random random = new Random();
        string tag = Instance.tags[random.Next(Instance.tags.Length)];
        return Instance.CreateCard(tag, playerId, regionId);
    }
}
