using Godot;
using System;
using MoonSharp.Interpreter;

public class Card : Node2D
{
    private long idCount = 0;

    public long Id { get; private set; }
    public string Tag { get; private set; }

    public CardType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }

    public byte Attack { get; private set; }
    public byte Defence { get; private set; }

    public string ScriptPath => $"data/cards/{Tag}.lua";
    public MoonSharp.Interpreter.Script Script { get; private set; }

    private bool isInitialized;

    public void Initialize(string tag)
    {
        isInitialized = true;
        Id = idCount++;
        Tag = tag;
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

        if (!Script.Globals.Get("OnReady").IsNil())
            Script.Call(Script.Globals["OnReady"]);
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
