using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace TerraTrial.Content.Subworlds;

static class PointExtensions
{
    internal static int DistanceSquared(this Point p1, Point p2)
    {
        return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
    }

    internal static float Distance(this Point p1, Point p2)
    {
        return MathF.Sqrt(p1.DistanceSquared(p2));
    }    
}

internal class RectangleToPoint(Point endPoint, int baseWidth) : GenShape
{
    public override bool Perform(Point origin, GenAction action)
    {
        var dx = endPoint.X - origin.X;
        var dy = endPoint.Y - origin.Y;

        var stepVector = new Vector2(dx, dy).SafeNormalize(default) / 2f;
        var tangentVector = stepVector.RotatedBy(MathF.PI / 2);
        var steps = (int)origin.Distance(endPoint);
        var startPos = new Vector2(origin.X, origin.Y);

        for (var i = -2; i < steps * 2 + 2; i++)
        {
            for(var j = -baseWidth; j < baseWidth; j++)
            {
                var currentPos = startPos + i * stepVector + j * tangentVector;
                var currentPoint = new Point((int)currentPos.X, (int)currentPos.Y);
                if (currentPoint.X < 0 || currentPoint.Y < 0 || currentPoint.X >= Main.maxTilesX ||
                    currentPoint.Y >= Main.maxTilesY)
                {
                    continue;
                }
                if (!UnitApply(action, origin, currentPoint.X, currentPoint.Y) && _quitOnFail)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

internal class ClearNonimportantTiles : GenAction
{
    public override bool Apply(Point origin, int x, int y, params object[] args)
    {
        var tile = Main.tile[x, y];
        if (!Main.tileFrameImportant[tile.TileType])
        {
            WorldUtils.ClearTile(x, y, true);
        }
        return true;
    }
}

internal class WorldZonesModSystem() : ModSystem
{
    // When considering world zones, downsample tiles to "player-sized" chunks
    public int XDownSample => 2;

    public int YDownSample => 2;
    
    public int[,] Zones { get; private set; }
    
    // Down-sampled 2D array of tiles in the world, true if tile is not passable by player
    public bool[,] SolidsMask { get; private set;  }
    
    private readonly Point[] _cardinals = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
    private readonly ushort[] _wallIds =
    [
        WallID.RubyGemspark,
        WallID.SapphireGemspark,
        WallID.EmeraldGemspark,
        WallID.AmethystGemspark,
        WallID.TopazGemspark,
        WallID.AmberGemspark,
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
    private void LabelZone(Point startPoint, int label)
    {
        var fillQueue = new Stack<Point>();
        fillQueue.Push(startPoint);
        Zones[startPoint.X, startPoint.Y] = label;

        while (fillQueue.Count > 0)
        {
            var current = fillQueue.Pop();
            foreach (var (c1, c2) in _cardinals)
            {
                var x = current.X + c1;
                var y = current.Y + c2;
                if (x <= 0 || y < 0 || x >= Zones.GetLength(0) || y >= Zones.GetLength(1))
                {
                    continue;
                }
                if (SolidsMask[x, y] || Zones[x, y] != 0) continue;
                fillQueue.Push(new Point(x, y));
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

    private IEnumerable<Point> IterateZones()
    {
        for (var i = 0; i < Zones.GetLength(0); i++)
        {
            for (var j = 0; j < Zones.GetLength(1); j++)
            {
                yield return new Point(i, j);
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
    
    private Dictionary<int, List<Point>> GenerateEdgeLists(List<int> rankedZones, int zonesToShow)
    {
        var zonesToConsider = rankedZones.Take(zonesToShow).ToList();
        var edgeLists = new Dictionary<int, List<Point>>();
        foreach (var (i, j) in IterateZones())
        {
            var label = Zones[i, j];
            if (!zonesToConsider.Contains(label) || !IsEdgeTile(i, j)) continue;
            if (edgeLists.TryGetValue(label, out var edgeList))
            {
                edgeList.Add(new Point(i, j));
            }
            else
            {
                edgeLists[label] = [new Point(i, j)];
            }
        }

        return edgeLists;
    }

    private bool TryFindZoneNearpoints(List<Point> edge1, List<Point> edge2, out Point minP1, out Point minP2, int maxDistSq = 100*100)
    {
        minP1 = default;
        minP2 = default;
        var minDistSq = int.MaxValue;
        foreach (var p1 in edge1)
        {
            foreach (var p2 in edge2)
            {
                var dist = p1.DistanceSquared(p2);
                if (dist < minDistSq && dist < maxDistSq)
                {
                    minDistSq = dist;
                    minP1 = p1;
                    minP2 = p2;
                }
            }
        }
        return minP1 != default && minP2 != default;
    }
    
    private void ClearTilesInDiagonal(Point startPoint, Point endPoint) {}
    /// <summary>
    /// Given a list of the edges of the contiguous zones of the map, find the
    /// points of other contiguous zones which come the closest to intersecting
    /// </summary>
    /// <param name="edgeLists"></param>
    private List<(Point, Point)> FindAllZoneNearpoints(Dictionary<int, List<Point>> edgeLists, int maxDist = 100)
    {
        var nearPoints = new List<(Point, Point)>();
        var edgesList = edgeLists.Values.ToList();
        for (var i = 0; i < edgesList.Count; i++)
        {
            for (var j = i + 1; j < edgesList.Count; j++)
            {
                var edge1 = edgesList[i];
                var edge2 = edgesList[j];
                if (edge1 != edge2 && TryFindZoneNearpoints(edge1, edge2, out var p1, out var p2, maxDist * maxDist))
                {
                    nearPoints.Add((p1, p2));
                }
            }
        }
        return nearPoints;
    }
    
    private void ClearTilesBetweenNearpoints(List<(Point, Point)> nearPoints) 
    {
        foreach (var (start, end) in nearPoints)
        {
            var worldCoordsStart = new Point(start.X * XDownSample, start.Y * YDownSample);
            var worldCoordsEnd = new Point(end.X * XDownSample, end.Y * YDownSample);
            WorldUtils.Gen(
                worldCoordsStart, 
                new RectangleToPoint(worldCoordsEnd, 4), 
                new ClearNonimportantTiles());
        }
        
    }

    private void DebugAddWallsToEdges(Dictionary<int, List<Point>> edges)
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
    
    private void DebugAddWallsToZoneNearPoints(List<(Point, Point)> nearPoints)
    {
        foreach (var ((x1, y1), (x2, y2)) in nearPoints)
        {
            for (var i = 0; i < XDownSample; i++)
            {
                for (var j = 0; j < YDownSample; j++)
                {
                    var tile = Main.tile[x1 * XDownSample + i, y1 * YDownSample + j];
                    tile.WallType = WallID.DiamondGemspark;
                    tile = Main.tile[x2 * XDownSample + i, y2 * YDownSample + j];
                    tile.WallType = WallID.DiamondGemspark;
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
            LabelZone(new Point(x, y), zoneLabel);
            zoneLabel++;
        }
        Mod.Logger.Info($"Generated zones for {zoneLabel} chests");
        
        var rankedZones = RankZoneSizes(zoneLabel);
        
        // DebugAddWallsToZones(rankedZones, zoneLabel - 2);

        var edges = GenerateEdgeLists(rankedZones, zoneLabel - 2);

        var nearPoints = FindAllZoneNearpoints(edges, 30);
        
        ClearTilesBetweenNearpoints(nearPoints);
        
        DebugAddWallsToEdges(edges);
        
        DebugAddWallsToZoneNearPoints(nearPoints);
    }

    
}

internal class ConnectLargeZonesGenPass() : GenPass("Connect Zones", 1)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        ModContent.GetInstance<WorldZonesModSystem>().ApplyPass(progress, configuration);
    }
}
