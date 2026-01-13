using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;

internal class ConnectLargeZonesGenPass() : GenPass("Connect Zones", 1)
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
