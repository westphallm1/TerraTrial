using System;
using System.Collections.Generic;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.NPCs;

public class SpawnRateGlobalNPC : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if(!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        Main.NewText($"{spawnRate} {maxSpawns}");
        // More Spawns by default. TODO decrease spawns as player stays in
        // a single location
        spawnRate = Math.Min(75, spawnRate / 3);
        maxSpawns = Math.Max(12, maxSpawns * 2);
    }
}