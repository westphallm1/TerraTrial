using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.Players;

public readonly struct TimedPlayerPos(Vector2 position, TimeSpan time)
{
    public Vector2 Position { get; } = position;
    public TimeSpan Time { get; } = time;
}
/// <summary>
/// Util ModPlayer that tracks the distance that a player has traveled. Used
/// to modify spawn rates, etc. for the purpose of 
/// </summary>
public class DistanceTrackingPlayer : ModPlayer
{
    /// <summary>
    /// Distance at which to add a new player position to the list
    /// </summary>
    private const int UpdateDist = 8;

    /// <summary>
    /// Length of time for which to retain player location records
    /// </summary>
    private TimeSpan RetainTime = TimeSpan.FromMinutes(2);
    public Queue<TimedPlayerPos> PlayerPositions { get; } = [];

    /// <summary>
    /// If the player has traveled far enough since the last location was recorded,
    /// record a new entry in the list. Also, remove old entries
    /// </summary>
    public override void PostUpdate()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;

        if (PlayerPositions.Count == 0 || Player.Center.DistanceSQ(PlayerPositions.Last().Position) > UpdateDist * UpdateDist)
        {
           PlayerPositions.Enqueue(new TimedPlayerPos(Player.Center, Main.gameTimeCache.TotalGameTime)); 
        }

        if (Main.gameTimeCache.TotalGameTime - PlayerPositions.First().Time > RetainTime)
        {
            PlayerPositions.Dequeue();
        }

        var bounds = new Rectangle((int)Player.Center.X - 960, (int)Player.Center.Y - 540, 1920, 1080);
        Main.NewText($"{(int)MathF.Sqrt(MaxDistSqTravelled(TimeSpan.FromSeconds(15)))}, {FirstTimeInBounds(bounds, TimeSpan.FromMinutes(2), TimeSpan.Zero)}");
    }

    /// <summary>
    /// Given a time span, return the farthest that the player has traveled
    /// from their current position within that timespan
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public float MaxDistSqTravelled(TimeSpan time)
    {
        var now = Main.gameTimeCache.TotalGameTime;
        return PlayerPositions
            .Where(p => now - p.Time <= time)
            .Select(p => p.Position.DistanceSQ(Player.Center))
            .Max();
    }

    public TimeSpan FirstTimeInBounds(Rectangle bounds, TimeSpan from, TimeSpan to)
    {
        var now = Main.gameTimeCache.TotalGameTime;
        var firstVisited = PlayerPositions
            .Where(p => now - p.Time > to && now - p.Time < from)
            .FirstOrDefault(p => bounds.Contains((int)p.Position.X, (int)p.Position.Y)).Time;
        if (firstVisited != TimeSpan.Zero)
        {
            return now - firstVisited;
        }
        else
        {
            return TimeSpan.Zero;
        }
    }
}