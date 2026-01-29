using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using TerraTrial.Content.Players;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.NPCs;

public class SpawnRateGlobalNpc : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if(!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        var bounds = new Rectangle((int)player.Center.X - 960, (int)player.Center.Y - 540, 1920, 1080);
        // If the player first entered this zone under 15 seconds ago, or over two minutes ago
        if (player.GetModPlayer<DistanceTrackingPlayer>()
                .FirstTimeInBounds(bounds, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(15)) == TimeSpan.Zero)
        {
            // More spawns by default.
            // a single location
            spawnRate = Math.Min(75, spawnRate / 3);
            maxSpawns = Math.Max(12, maxSpawns * 2);
        }
        else
        {
            // Fewer spawns after a player has spent time in a zone
            spawnRate *= 2;
            maxSpawns /= 2;
        }
    }
}