using Godot;
using System;

public class PathManager : Node2D
{
    private static PathManager instance;

    private Sprite path;

    public PathManager()
    {
        if (instance != null) return;
        instance = this;

        PackedScene scene = GD.Load("path/path.tscn") as PackedScene;
        path = scene.Instance() as Sprite;
        AddChild(path);
    }

    public static void DrawPath(Vector2 from, Vector2 to)
    {
        if (instance == null)
        {
            GD.PushError($"Coudln't draw path because PathManager isn't in scene yet.");
            return;
        }

        instance.path.GlobalPosition = from;
        instance.path.GlobalScale = new Vector2(from.DistanceTo(to), 15);
        instance.path.LookAt(to);
        instance.path.RotationDegrees = instance.path.RotationDegrees + 180;
        instance.path.Visible = true;
    }

    public static void HidePath()
    {
        if (instance == null)
        {
            GD.PushError($"Coudln't hide path because PathManager isn't in scene yet.");
            return;
        }
        instance.path.Visible = false;
    }
}
