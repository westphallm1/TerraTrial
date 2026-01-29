using System;
using SubworldLibrary;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.Players;

public class TerraTrialPlayer : ModPlayer
{
    // consts that determine behavior-changing inflection points
    public const int JumpDouble = 0;
    public const int JumpBoots = 5;
    public const int JumpWings = 10;

    public const int SpeedBoots = 3;
    
    
    internal int Speed { get; set; } 
    internal int Jump { get; set; }
    public int Health { get; set; }
    
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Acceleration { get; set; }
    public int Weight { get; set; }

    public override void OnEnterWorld()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        // reset the player's items
        // TODO make super sure that this doesn't save
        Player.DropItems();
        
        // Clear out the default hotbar items given by DropItems()
        for (var i = 0; i < 9; i++)
        {
            Player.inventory[i].TurnToAir();
        }
        
        // Give a new set of default
        Player.inventory[0].SetDefaults(ItemID.GoldBroadsword);
        Player.inventory[0].stack = 1;

        Player.miscEquips[4].SetDefaults(ItemID.GrapplingHook);
        Player.miscEquips[4].stack = 1;

        Player.ConsumedLifeCrystals = 0;
        Player.ConsumedLifeFruit = 0;
        Player.ConsumedManaCrystals = 0;
    }

    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        base.ModifyMaxStats(out health, out mana);
        if (SubworldSystem.IsActive<TerraTrialWorld>())
        {
            health.Base = 10 * Health;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        Player.moveSpeed += 0.05f + MathF.Min(20, Speed) / 25f;
        Player.jumpSpeedBoost += MathF.Min(Jump, 5) / 5f;

        if (Speed >= SpeedBoots)
        {
            Player.accRunSpeed = 6.75f;
            Player.armor[14].SetDefaults(ItemID.HermesBoots);
        }
        
        if (Jump is >= JumpDouble and < JumpWings)
        {
            Player.GetJumpState<StatScalingExtraJump>().Enable();
            Player.armor[13].SetDefaults(ItemID.CloudinaBottle);
        }

        if (Jump >= JumpBoots)
        {
            Player.rocketBoots = 2;
            Player.vanityRocketBoots = 2;
            Player.armor[14].SetDefaults(ItemID.RocketBoots);
        }

        if (Jump >= JumpWings)
        {
            Player.wingsLogic = 1;
            // Need to have a real wings in a real wing slot to trigger wing logic
            Player.armor[5].SetDefaults(ItemID.AngelWings);
            Player.armor[15].SetDefaults(ItemID.AngelWings);
        }
        
        // QOL features, autoswing and no fall damage
        Player.autoReuseAllWeapons = true;
        Player.noFallDmg = true;
        
        
    }

    public override void PostUpdateRunSpeeds()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        base.PostUpdateRunSpeeds();
        Player.runAcceleration *= 1.5f;
        Player.runSlowdown *= 1.5f;
        Player.maxRunSpeed *= 1.15f;
        Player.accRunSpeed *= 1.15f;
    }
}

/// <summary>
/// Collection of On_Player. hooks that detour vanilla movement-calculating code
/// with Terra Trial stat-specific behavior
/// </summary>
public class PlayerStatScalingModSystem : ModSystem
{
    public override void Load()
    {
        On_Player.GetWingStats += On_PlayerOnGetWingStats;
    }

    private WingStats On_PlayerOnGetWingStats(On_Player.orig_GetWingStats orig, Player self, int wingId)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>() || self.GetModPlayer<TerraTrialPlayer>() is var modPlayer 
            && modPlayer.Jump < TerraTrialPlayer.JumpWings)
        {
            return orig(self, wingId);
        }

        return new WingStats(
            10 * modPlayer.Jump,
            3f + modPlayer.Jump / 3f,
            modPlayer.Jump / 6f
        );
    }
}
