using Godot;
using System;
using MoonSharp.Interpreter;

public class CardData
{
    public string Tag { get; private set; }

    public CardType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }

    public ushort Price { get; private set; }
    public ushort Upkeep { get; private set; }

    public byte Attack { get; private set; }
    public byte Defence { get; private set; }

    public string ScriptPath => $"data/cards/{Tag}.lua";
    public MoonSharp.Interpreter.Script Script { get; private set; }

    public CardData(string tag)
    {
        Tag = tag;
        SetScript();
        GetScriptData();
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
}
