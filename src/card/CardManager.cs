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
    }

    private void GetCards()
    {
        cards = new Dictionary<string, ushort>();
        foreach (var tag in Utils.ContentUtils.GetScriptNamesOnFolder(CardsPath))
        {
            cards.Add(tag, 0);
        }
    }
}
