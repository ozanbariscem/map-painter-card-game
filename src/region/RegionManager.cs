﻿using Godot;
using System.Collections.Generic;

public class RegionManager : Node2D
{
    private static RegionManager Instance;

    [Export] public byte MapWidth;
    [Export] public byte MapHeight;

    public Dictionary<int, Region> Regions;

    public RegionManager()
    {
        if (Instance != null) return;
        Instance = this;
        Regions = new Dictionary<int, Region>();
    }

    public override void _Ready()
    {
        CreateMap(MapWidth, MapHeight);
    }

    private void CreateMap(byte width, byte height)
    {
        PackedScene scene = GD.Load("region/region.tscn") as PackedScene;
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                Region region = scene.Instance() as Region;
                region.SetNeighbours(GetNeighbours(region.Id));
                // Note that this would be completely irrelevant 
                // If we were to have other region shapes than squares
                Sprite border = region.GetNode("visuals/border") as Sprite;
                region.GlobalPosition = new Vector2(border.Texture.GetWidth() * i, border.Texture.GetHeight() * j);

                Regions.Add(region.Id, region);
                AddChild(region);
            }
        }
    }

    /// <summary>
    /// This function only works if map is generated by the CreateMap() function
    /// And the map is generated row by row, meaning consecutive regions are also consecutive in ids
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private int[] GetNeighbours(int id)
    {
        List<int> neighbours = new List<int>();

        // Right / Left
        if (IdIsInMap(id + 1) && id + 1 >= 0) neighbours.Add(id + 1);
        if (IdIsInMap(id - 1) && id - 1 >= 0) neighbours.Add(id - 1);
        // Top
        if (IdIsInMap(id - MapWidth) && id - MapWidth >= 0) neighbours.Add(id - MapWidth);
        // Top Right / Left
        if (IdIsInMap(id - MapWidth + 1) && id - MapWidth + 1 >= 0) neighbours.Add(id - MapWidth + 1);
        if (IdIsInMap(id - MapWidth - 1) && id - MapWidth - 1 >= 0) neighbours.Add(id - MapWidth - 1);
        // Bot
        if (IdIsInMap(id + MapWidth) && id + MapWidth >= 0) neighbours.Add(id + MapWidth);
        // Bot Right / Left
        if (IdIsInMap(id + MapWidth + 1) && id + MapWidth + 1 >= 0) neighbours.Add(id + MapWidth + 1);
        if (IdIsInMap(id + MapWidth - 1) && id + MapWidth - 1 >= 0) neighbours.Add(id + MapWidth - 1);

        return neighbours.ToArray();
    }

    public static bool IdIsInMap(int id)
    {
        if (Instance == null) return false;
        return id < Instance.MapHeight * Instance.MapWidth;
    }

    public static Region GetRegion(int id)
    {
        if (Instance == null)
        {
            GD.PushError($"Couldn't get region with id {id}. Region manager isn't in scene yet.");
            return null;
        }

        if (!Instance.Regions.TryGetValue(id, out var region))
        {
            GD.PushError($"Couldn't get region with id {id}. Regions doesn't contain region with this id.");
            return null;
        }

        return region;
    }
}
