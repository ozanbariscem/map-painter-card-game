using Godot;
using System;
using System.Collections.Generic;

public static class ColorUtils
{
    public static Dictionary<CardType, Color> CardColors = new Dictionary<CardType, Color>()
    {
        { CardType.Military, new Color("D3DEDC") }
    };

    private static List<Color> playerColors = new List<Color>()
    {
        new Color("C2DED1"),
        new Color("ECE5C7"),
        new Color("CDC2AE"),
        new Color("354259"),
        new Color("9A86A4"),
        new Color("B1BCE6"),
        new Color("B7E5DD"),
        new Color("F1F0C0"),
        new Color("F8ECD1"),
        new Color("DEB6AB"),
        new Color("AC7D88"),
        new Color("85586F"),
        new Color("F1DDBF"),
        new Color("525E75"),
        new Color("78938A"),
        new Color("92BA92"),
    };

    public static Color GetRandomPlayerColor()
    {
        Random random = new Random();
        int rng = random.Next(playerColors.Count);
        Color color = playerColors[rng];
        playerColors.RemoveAt(rng);
        return color;
    }
}
