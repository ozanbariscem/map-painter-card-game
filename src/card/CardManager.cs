using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class CardManager : Node2D
{
    public static event Action<List<CardData>> OnCardDatasInitialized;

    private static CardManager Instance;

    public string CardsPath => $"data/cards";

    private Dictionary<string, CardData> datas;
    private string[] tags;

    private YSort ySort;

    public CardManager()
    {
        if (Instance != null) return;
        Instance = this;
    }

    public override void _Ready()
    {
        ySort = GetNode("YSort") as YSort;
        GetCards();
    }

    private void GetCards()
    {
        datas = new Dictionary<string, CardData>();
        foreach (var tag in Utils.ContentUtils.GetScriptNamesOnFolder(CardsPath))
        {
            datas.Add(tag, new CardData(tag));
        }
        tags = datas.Keys.ToArray();
        OnCardDatasInitialized?.Invoke(datas.Values.ToList());
    }

    public static Card CreateCard(string tag, ulong holderId, int regionId)
    {
        if (Instance == null)
        {
            GD.PushError($"Can't create card because there are no CardManagers active in the scene.");
            return null;
        }

        if (!Instance.datas.TryGetValue(tag, out var data)) return null;

        PackedScene scene = GD.Load("card/card.tscn") as PackedScene;
        Card card = scene.Instance() as Card;
        card.Initialize(tag, data, holderId, regionId);
        Instance.ySort.AddChild(card);

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
        return CreateCard(tag, playerId, regionId);
    }
}
