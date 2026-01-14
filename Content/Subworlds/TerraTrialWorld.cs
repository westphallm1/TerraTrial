using System;
using System.Collections.Generic;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;

public class TerraTrialWorldSystem : ModSystem
{
    public int Width => 2000;

    public int Height => 900;
    
    public int[,] Zones { get; private set; }
    
    public override void Load()
    {
        ResetZones();
    }

    public void ResetZones()
    {
        Zones = new int[Width, Height];
    }
    
    public override void Unload()
    {
        Zones = null;
    }
}
public class TerraTrialWorld : Subworld
{
    public override int Width => ModContent.GetInstance<TerraTrialWorldSystem>().Width;
    public override int Height => ModContent.GetInstance<TerraTrialWorldSystem>().Height;

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
            "Micro Biomes",
            "Spider Caves"
        ];
        foreach (var layer in toRemove)
        {
            var idx = tasks.FindIndex(t => t.Name == layer);
            tasks.RemoveAt(idx);
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
        
        Mod.Logger.Info($"Kept {tasks.Count} tasks");
    }
}