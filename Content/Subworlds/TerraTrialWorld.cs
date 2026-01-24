using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;


public class TerraTrialWorld : Subworld
{
    public const int WorldWidth = 2000;

    public const int WorldHeight = 900;
    public override int Width => WorldWidth;
    public override int Height => WorldHeight;

    public override bool NoPlayerSaving => true;
    public override bool ShouldSave => false;

    public override List<GenPass> Tasks => [
        new SeedGenPass(),
        new StandardWorldGenPass(),
        new ConnectLargeZonesGenPass(),
    ];
    
    public override void OnEnter()
    {
        SubworldSystem.hideUnderworld = false;
    }

    private static void SafeMultColor(ref Vector3 color, float threshold = 0.5f)
    {
        if (!((color.X + color.Y + color.Z) / 3f < threshold)) return;
        color.X = Math.Max(color.X, threshold);
        color.Y = Math.Max(color.Y, threshold);
        color.Z = Math.Max(color.Z, threshold);

    }

    public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
    {
        // light up tiles near the player that are inside the explorable zone
        const int lightDist = 16;
        var system = ModContent.GetInstance<WorldZonesModSystem>();
        var zones = system.Zones;
        var xIdx = x / system.XDownSample;
        var yIdx =  y / system.YDownSample;
        var player = Main.LocalPlayer;
        if (zones[xIdx, yIdx] != 0 && 
            (player.position / 16).DistanceSQ(new Vector2(x, y)) < lightDist * lightDist)
        {
            SafeMultColor(ref color);
            return true;
        } 
        if (zones[xIdx, yIdx] != 0)
        {
            // Give very dim glow to all traversable tiles
            SafeMultColor(ref color, 0.15f);
        }
        if (tile is { HasTile: true, TileType: TileID.Containers })
        {
            // TODO different color based on chest type
            color = Vector3.One;
            return true;
        }
        return base.GetLight(tile, x, y, ref rand, ref color);
    }
}

internal class SeedGenPass() : GenPass("Set Seed", 0.01f)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = "Setting subworld seed";
        Main.ActiveWorldFileData.SetSeedToRandom();
    }
}

internal class StandardWorldGenPass() : GenPass("Standard World", 100)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        var cache = WorldGenerator.CurrentGenerationProgress;
        WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed);
        
        WorldGenerator.CurrentGenerationProgress = cache;
    }
}


public class GenPassWrapper(GenPass other) : GenPass(other.Name, other.Weight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        ModContent.GetInstance<TerraTrial>().Logger.Info($"Starting GenPass {Name}");
        // Some passes (mainly the initial reset) can error out with a small world size, try to brute force it
        for (var _ = 0; _ < 5; _++)
        {
            try
            {
                other.Apply(progress, configuration);
                break;
            }
            catch (Exception e)
            {
                ModContent.GetInstance<TerraTrial>().Logger.Error("Exception during tiny world generation", e);
                // continue
            } 
        }
        ModContent.GetInstance<TerraTrial>().Logger.Info($"Finishing GenPass {Name}");
    }
}

public class RemoveCaveWaterGenPass() : GenPass("Remove Cave Water", 0.01f)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        for (var i = 0; i < Main.maxTilesX; i++)
        {
            for (var k = Main.spawnTileY; k < Main.maxTilesY; k++)
            {
                var tile = Main.tile[i, k];
                if (tile is { LiquidType: LiquidID.Water, LiquidAmount: > 0 })
                {
                    tile.LiquidAmount = 0;
                }
            }
        }
    }
}

public class TrialWorldVanillaGen : ModSystem
{
    public override void OnModLoad()
    {
        On_WorldGen.makeTemple += On_WorldGenOnmakeTemple;
        On_WorldGen.ShimmerCleanUp += On_WorldGenOnShimmerCleanUp;
        
    }
    // We don't need a shimmer in the super-small map
    private void On_WorldGenOnShimmerCleanUp(On_WorldGen.orig_ShimmerCleanUp orig)
    {
        if(SubworldSystem.IsActive<TerraTrialWorld>())
        {
            return;
        }
        orig.Invoke();
    }

    // We don't need a jungle temple in the super-small map
    private void On_WorldGenOnmakeTemple(On_WorldGen.orig_makeTemple orig, int x, int y)
    {
        if(SubworldSystem.IsActive<TerraTrialWorld>())
        {
            return;
        }
        orig.Invoke(x, y);
    }

    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>())
        {
            return;
        }
        
        // Remove the oceans, underworld, shimmer, corruption, and jungle temple from the list of generation tasks
        List<string> toRemove =
        [
            // GenPasses for unnecessary features
            "Ocean Sand",
            "Create Ocean Caves",
            // "Corruption",
            "Guide",
            "Temple",
            "Hellforge",
            "Lihzahrd Altars",
            "Shimmer",
            "Shell Piles",
            "Beaches",
            
            // Relatively minor GenPasses that take >= 3 seconds :(
            // "Small Holes",
            // "Micro Biomes",
            "Spider Caves",
            
            // Webs slow down movement too much
            "Webs",
            
            // Hives serve no purpose for this
            "Hives",
        ];
        
        // We want to double the creation of chest-placing world gen steps for increased interact-ables
        List<string> toDouble = 
        [
            "Jungle Chests",
            "Buried Chests",
            "Surface Chests",
        ];
        
        foreach (var layer in toRemove)
        {
            var idx = tasks.FindIndex(t => t.Name == layer);
            tasks.RemoveAt(idx);
        }

        foreach (var layer in toDouble)
        {
            var idx = tasks.FindIndex(t => t.Name == layer);
            tasks.Insert(idx + 1, tasks[idx]);
        }
        
        // For debugging, wrap vanilla GenPasses in additional logging
        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i] = new GenPassWrapper(tasks[i]);
        }
        
        // Add a GenPass to space out structures further post-reset since there's no oceans
        var resetIdx = tasks.FindIndex(t => t.Name == "Reset");
        tasks.Insert(resetIdx + 1, new UpdateLocationsNoOceansGenPass());
        
        // Add a GenPass to remove cave water prior to settling liquids
        var liquidIdx = tasks.FindIndex(t => t.Name == "Settle Liquids");
        tasks.Insert(liquidIdx, new RemoveCaveWaterGenPass());
        
        liquidIdx = tasks.FindIndex(t => t.Name == "Settle Liquids Again");
        tasks.Insert(liquidIdx, new RemoveCaveWaterGenPass());
        
        Mod.Logger.Info($"Kept {tasks.Count} tasks");
    }
}