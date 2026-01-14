using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;

internal class ConnectLargeZonesGenPass() : GenPass("Connect Zones", 1)
{
    private static bool IsSolid(Tile t) => t.HasTile && t.TileType != TileID.ClosedDoor && Main.tileSolid[t.TileType] && ! Main.tileSolidTop[t.TileType];

    private const int WorldEdgeBound = 20;
    
    /// <summary>
    /// Given a starting coordinate and a label, assign the label to all "air" tiles that are contiguous with
    /// the start point.
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="label"></param>
    private static void LabelZone((int, int) startPoint, int label)
    {
        var worldSys = ModContent.GetInstance<TerraTrialWorldSystem>();
        var zones = worldSys.Zones;
        
        var fillQueue = new Stack<(int, int)>();
        fillQueue.Push(startPoint);
        zones[startPoint.Item1, startPoint.Item2] = label;

        var offsets = new [] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        while (fillQueue.Count > 0)
        {
            var current = fillQueue.Pop();
            for(var idx = 0; idx < offsets.Length; idx ++)
            {
                var x = current.Item1 + offsets[idx].Item1;
                var y = current.Item2 + offsets[idx].Item2;
                if (x <= WorldEdgeBound || y < WorldEdgeBound || x >= worldSys.Width - WorldEdgeBound || y >= worldSys.Height - WorldEdgeBound)
                {
                    continue;
                }
                var tile = Main.tile[x, y];
                if (IsSolid(tile) || zones[x, y] != 0) continue;
                fillQueue.Push((x, y));
                zones[x, y] = label;
            }
        }
    }

    /// <summary>
    /// Return a list of labeled zones, ordered by 
    /// </summary>
    /// <param name="zoneCount"></param>
    /// <returns></returns>
    public List<int> RankZoneSizes(int zoneCount)
    {
        var worldSys = ModContent.GetInstance<TerraTrialWorldSystem>();
        var zones = worldSys.Zones;

        var zoneSizes = new int[zoneCount + 1];

        for (var i = 1; i < Main.maxTilesX - 1; i++)
        {
            for (var j = 1; j < Main.maxTilesY - 1; j++)
            {
                zoneSizes[zones[i, j]]++;
            }
        }

        // We don't care about unlabeled zones
        zoneSizes[0] = 0;
        return zoneSizes.Select((val, idx) => new { val, idx })
            .OrderByDescending(a => a.val)
            .Select(a => a.idx)
            .ToList();
    }


    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        var worldSys = ModContent.GetInstance<TerraTrialWorldSystem>();
        worldSys.ResetZones();
        var zones = worldSys.Zones;
        
        var zoneLabel = 1;
        for (var i = WorldEdgeBound; i < Main.maxTilesX - WorldEdgeBound; i++)
        {
            for (var j = WorldEdgeBound; j < Main.maxTilesY - WorldEdgeBound; j++)
            {
                if (zones[i, j] != 0 || IsSolid(Main.tile[i, j])) continue;
                LabelZone((i, j), zoneLabel);
                zoneLabel++;
            }
        }

        var rankedZones = RankZoneSizes(zoneLabel);
        for (var i = 1; i < Main.maxTilesX - 1; i++)
        {
            for (var j = 1; j < Main.maxTilesY - 1; j++)
            {
                ushort[] wallIds =
                [
                    WallID.RubyGemspark,
                    WallID.SapphireGemspark,
                    WallID.EmeraldGemspark,
                    WallID.AmethystGemspark,
                    WallID.TopazGemspark,
                    WallID.AmberGemspark,
                    WallID.DiamondGemspark
                ];
                for (var w = 0; w < wallIds.Length; w++)
                {
                    if (zones[i, j] != rankedZones[w]) continue;
                    var tile = Main.tile[i, j];
                    tile.WallType = wallIds[w];
                    break;
                }
            }
        }
    }
}
