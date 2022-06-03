using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class MarketManager : Node2D
{
    private List<CardData> datas;

    public override void _Ready()
    {
        base._Ready();
        CardManager.OnCardDatasInitialized += HandleCardDatasInitialized;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CardManager.OnCardDatasInitialized -= HandleCardDatasInitialized;
    }

    private void HandleCardDatasInitialized(List<CardData> datas)
    {
        this.datas = datas;
    }
}
