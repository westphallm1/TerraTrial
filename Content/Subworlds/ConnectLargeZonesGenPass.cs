using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;

internal class WorldZonesModSystem() : ModSystem
{
    // When considering world zones, downsample tiles to "player-sized" chunks
    public int XDownSample => 2;

    public int YDownSample => 2;
    
    public int[,] Zones { get; private set; }
    
    // Down-sampled 2D array of tiles in the world, true if tile is not passable by player
    public bool[,] SolidsMask { get; private set;  }
    
    private readonly (int, int)[] _cardinals = [(-1, 0), (1, 0), (0, -1), (0, 1)];
    private readonly ushort[] _wallIds =
    [
        WallID.RubyGemspark,
        WallID.SapphireGemspark,
        WallID.EmeraldGemspark,
        WallID.AmethystGemspark,
        WallID.TopazGemspark,
        WallID.AmberGemspark,
        WallID.DiamondGemspark
    ];

    
    
    private static bool IsSolid(Tile t) => t.HasTile && t.TileType != TileID.ClosedDoor && Main.tileSolid[t.TileType] && ! Main.tileSolidTop[t.TileType];
    
    public override void Load()
    {
        ResetZones();
    }

    public void ResetZones()
    {
        Zones = new int[TerraTrialWorld.WorldWidth / XDownSample, TerraTrialWorld.WorldHeight / YDownSample];
        SolidsMask = new bool[TerraTrialWorld.WorldWidth / XDownSample, TerraTrialWorld.WorldHeight / YDownSample];
    }
    
    public override void Unload()
    {
        Zones = null;
        SolidsMask = null;
    }
    
    /// <summary>
    /// Given a starting coordinate and a label, assign the label to all "air" tiles that are contiguous with
    /// the start point.
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="label"></param>
    private void LabelZone((int, int) startPoint, int label)
    {
        var fillQueue = new Stack<(int, int)>();
        fillQueue.Push(startPoint);
        Zones[startPoint.Item1, startPoint.Item2] = label;

        while (fillQueue.Count > 0)
        {
            var current = fillQueue.Pop();
            foreach (var (c1, c2) in _cardinals)
            {
                var x = current.Item1 + c1;
                var y = current.Item2 + c2;
                if (x <= 0 || y < 0 || x >= Zones.GetLength(0) || y >= Zones.GetLength(1))
                {
                    continue;
                }
                if (SolidsMask[x, y] || Zones[x, y] != 0) continue;
                fillQueue.Push((x, y));
                Zones[x, y] = label;
            }
        }
    }
    
    /// <summary>
    /// Return a list of labeled zones, ordered by the size of that zone
    /// </summary>
    /// <param name="zoneCount"></param>
    /// <returns></returns>
    public List<int> RankZoneSizes(int zoneCount)
    {
        var worldSys = ModContent.GetInstance<WorldZonesModSystem>();
        var zones = worldSys.Zones;

        var zoneSizes = new int[zoneCount + 1];

        foreach (var (i, j) in IterateZones())
        {
            zoneSizes[zones[i, j]]++;
        }

        // We don't care about unlabeled zones
        zoneSizes[0] = 0;
        return zoneSizes.Select((val, idx) => new { val, idx })
            .OrderByDescending(a => a.val)
            .Select(a => a.idx)
            .ToList();
    }

    /// <summary>
    /// Populate the down-sampled boolean 2D array of whether or not each tile is traversable
    /// </summary>
    private void FillSolidsMask()
    {
        for (var i = 1; i < Main.maxTilesX - 1; i++)
        {
            for (var j = 1; j < Main.maxTilesY - 1; j++)
            {
                SolidsMask[i / XDownSample, j / YDownSample] |= IsSolid(Main.tile[i, j]);
            }
        }
    }

    private IEnumerable<(int, int)> IterateZones()
    {
        for (var i = 0; i < Zones.GetLength(0); i++)
        {
            for (var j = 0; j < Zones.GetLength(1); j++)
            {
                yield return (i, j);
            }
        }
    }

    /// <summary>
    /// Given a tile, return whether that tile has at least one non-matching neighbor
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private bool IsEdgeTile(int x, int y)
    {
        foreach (var (c1, c2) in _cardinals)
        {
            var x2 = x + c1;
            var y2 = y + c2;
            if(x2 < 0 || y2 < 0 ||
               x2 >= Zones.GetLength(0) || y2 >= Zones.GetLength(1) ||
               Zones[x,y] != Zones[x2,y2])
            {
                return true;
            }
        }

        return false;
    }
    
    private Dictionary<int, List<(int, int)>> GenerateEdgeLists(List<int> rankedZones, int zonesToShow)
    {
        var zonesToConsider = rankedZones.Take(zonesToShow).ToList();
        var edgeLists = new Dictionary<int, List<(int, int)>>();
        foreach (var (i, j) in IterateZones())
        {
            var label = Zones[i, j];
            if (!zonesToConsider.Contains(label) || !IsEdgeTile(i, j)) continue;
            if (edgeLists.TryGetValue(label, out var edgeList))
            {
                edgeList.Add((i, j));
            }
            else
            {
                edgeLists[label] = [(i, j)];
            }
        }

        return edgeLists;
    }

    private void DebugAddWallsToZones(List<int> rankedZones, int zonesToShow)
    {
        foreach (var (i, j) in IterateZones())
        {
            for (var w = 0; w < zonesToShow; w++)
            {
                if (Zones[i, j] != rankedZones[w]) continue;
                    
                for (var i2 = 0; i2 < XDownSample; i2++)
                {
                    for (var j2 = 0; j2 < YDownSample; j2++)
                    {
                        var tile = Main.tile[i * XDownSample + i2, j * YDownSample + j2];
                        tile.WallType = _wallIds[w % _wallIds.Length];
                    }
                }
                break;
            }
        }
    }
    
    private void DebugAddWallsToEdges(Dictionary<int, List<(int, int)>> edges)
    {
        foreach (var (idx, edgeList) in edges)
        {
            foreach (var (x, y) in edgeList)
            {
                for (var i = 0; i < XDownSample; i++)
                {
                    for (var j = 0; j < YDownSample; j++)
                    {
                        var tile = Main.tile[x * XDownSample + i, y * YDownSample + j];
                        tile.WallType = _wallIds[idx % _wallIds.Length];
                    }
                }
            } 
        }
    }

    internal void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        ResetZones();
        FillSolidsMask();
        
        var zoneLabel = 1;
        
        // Fill zones starting from chests since that's a relatively reasonable metric for "interesting"
        foreach (var chest in Main.chest)
        {
            if(chest is null) continue;
            var x = chest.x / XDownSample;
            var y = chest.y / YDownSample;
            if (Zones[x, y] != 0 || SolidsMask[x, y]) continue;
            LabelZone((x, y), zoneLabel);
            zoneLabel++;
        }
        Mod.Logger.Info($"Generated zones for {zoneLabel} chests");
        
        /*
        foreach (var (i, j) in IterateZones())
        {
            if (Zones[i, j] != 0 || SolidsMask[i, j]) continue;
            LabelZone((i, j), zoneLabel);
            zoneLabel++;
        }
        */
        var rankedZones = RankZoneSizes(zoneLabel);
        
        // DebugAddWallsToZones(rankedZones, zoneLabel - 2);

        var edges = GenerateEdgeLists(rankedZones, zoneLabel - 2);
        
        DebugAddWallsToEdges(edges);

    }

    
}

internal class ConnectLargeZonesGenPass() : GenPass("Connect Zones", 1)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        ModContent.GetInstance<WorldZonesModSystem>().ApplyPass(progress, configuration);
    }
}
