using System;
using Terraria;
using Terraria.IO;
using Terraria.WorldBuilding;
using static Terraria.WorldGen;

namespace TerraTrial.Content.Subworlds;

/// <summary>
/// GenPass that updates the spacing of other biomes based on a lack of ocean
/// </summary>
public class UpdateLocationsNoOceansGenPass() : GenPass("Update Locations for No Oceans", 0.01f)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        // Jungle location
        GenVars.jungleOriginX = (int)(Main.maxTilesX * (GenVars.dungeonSide == -1 
                ? 1f - genRand.Next(5, 30) * 0.01f 
                : genRand.Next(5, 30) *  0.01f
            ));
            
        
        // Snow biome location
        var snowCenter = GenVars.dungeonSide == 1
            ? genRand.Next((int)(0.6f * Main.maxTilesX), (int)(0.85f * Main.maxTilesX))
            : genRand.Next((int)(0.15f * Main.maxTilesX), (int)(0.4f * Main.maxTilesX));
        var snowWidth1 = genRand.Next(50, 90) + 2 * (int)(genRand.Next(20, 40) * Main.maxTilesX / 4200.0);
        var snowWidth2 = genRand.Next(50, 90) + 2 * (int)(genRand.Next(20, 40) * Main.maxTilesX / 4200.0);
        GenVars.snowOriginLeft = Math.Max(0, snowCenter - snowWidth1);
        GenVars.snowOriginRight = Math.Min(Main.maxTilesX, snowCenter + snowWidth2);
        // Dungeon Location
        GenVars.dungeonLocation = GenVars.dungeonSide == -1
            ? genRand.Next(50, (int)(Main.maxTilesX * 0.2f))
            : genRand.Next((int)(Main.maxTilesX * 0.8f), Main.maxTilesX - 50);
    }
}