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
    public int Width => 1800;

    public int Height => 900;
    
    public int[,] Zones { get; private set; }
    
    public override void Load()
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
        new ZoneDiscoveryGenPass(),
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

internal class ZoneDiscoveryGenPass() : GenPass("Connect Zones", 1)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        var worldSys = ModContent.GetInstance<TerraTrialWorldSystem>();
        var zones = worldSys.Zones;
        
        (int, int) start = (Main.spawnTileX, Main.spawnTileY);
        var fillQueue = new Stack<(int, int)>();
        fillQueue.Push(start);

        ModContent.GetInstance<TerraTrial>().Logger.Info("Finding Contiguous Regions");
        var steps = 0;
        while (fillQueue.Count > 0)
        {
            var current = fillQueue.Pop();
            for(var i = -1; i <= 1; i+=2)
            {
                for(var j = -1; j <= 1; j+=2)
                {
                    steps += 1;
                    var x = current.Item1 + i;
                    var y = current.Item2 + j;
                    if (x <= 0 || y < 0 || x >= worldSys.Width || y >= worldSys.Height)
                    {
                        continue;
                    }
                    var tile = Main.tile[x, y];
                    if (tile is { HasTile: true, BlockType: BlockType.Solid } || zones[x, y] != 0) continue;
                    zones[x, y] = 1;
                    fillQueue.Push((x, y));
                }
            }
            
        }
        ModContent.GetInstance<TerraTrial>().Logger.Info($"Filled Queue in {steps} steps");
    }
}

public class GenPassWrapper(GenPass other) : GenPass(other.Name, other.Weight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        ModContent.GetInstance<TerraTrial>().Logger.Info($"Starting GenPass {Name}");
        other.Apply(progress, configuration);
        ModContent.GetInstance<TerraTrial>().Logger.Info($"Finishing GenPass {Name}");
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
            "Ocean Sand",
            "Create Ocean Caves",
            "Corruption",
            "Underworld",
            "Guide",
            "Temple",
            "Hellforge",
            "Lihzahrd Altars",
            "Shimmer",
            "Shell Piles",
            "Beaches",
        ];
        foreach (var layer in toRemove)
        {
            var idx = tasks.FindIndex(t => t.Name == layer);
            tasks.RemoveAt(idx);
        }

        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i] = new GenPassWrapper(tasks[i]);
        }
        
        Mod.Logger.Info($"Kept {tasks.Count} tasks");
    }
}