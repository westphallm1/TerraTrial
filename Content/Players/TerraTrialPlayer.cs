using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerraTrial.Content.Items;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.Players;

public class TerraTrialPlayer : ModPlayer
{
    // consts that determine behavior-changing inflection points
    public const int JumpDouble = 3;
    public const int JumpBoots = 6;
    public const int JumpWings = 9;
    
    
    internal int Speed { get; set; } 
    internal int Jump { get; set; }
    public int Health { get; set; }
    
    public int Attack { get; set; }

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
        Player.moveSpeed += 0.05f + MathF.Min(30, Speed) / 30f;
        Player.jumpSpeedBoost += MathF.Min(Jump, 5) / 5f;
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

public class ItemRemovalModSystem() : ModSystem
{

    private readonly List<Chest> _openedChests = [];
    
    public override void Load()
    {
        base.Load();
        On_Player.OpenChest += On_PlayerOnOpenChest;
    }

    private void On_PlayerOnOpenChest(On_Player.orig_OpenChest orig, Player self, int x, int y, int newChest)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>())
        {
            orig(self, x, y, newChest);
            return;
        }

        // Find the chest that the player opened and set it to open
        var chest = Main.chest[newChest];
        if(_openedChests.Contains(chest)) return;
        _openedChests.Add(chest);
        for (var i = 0; i < 3; i++)
        {
            var itemType = ItemPatch.PatchIDs[Main._rand.Next(ItemPatch.PatchIDs.Count)];
            var itemIdx = Item.NewItem(self.GetSource_FromThis(), new Vector2(x * 16, y * 16), itemType);
            Main.item[itemIdx].velocity = Vector2.UnitX.RotatedBy(- MathF.PI / 4 - MathF.PI * i /4) * Main._rand.Next(20, 40)/10f;
        }
        var player = self.GetModPlayer<TerraTrialPlayer>();
        Main.NewText($"Player stats updated! Speed: {player.Speed}, Jump {player.Jump}");
    }

    public override void PreUpdateItems()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        foreach (var item in Main.item)
        {
            if(item?.ModItem?.Mod == ModContent.GetInstance<TerraTrial>()) continue;
            item?.TurnToAir();
        }
    }

    public override void PreUpdateWorld()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        foreach (var chest in _openedChests)
        {
            chest.frame = 2;
            chest.frameCounter = 10;
        }
    }
}

public class ChestOpenGlobalItem : GlobalItem
{
    public override void UseItemHitbox(Item item, Player player, ref Rectangle hitbox, ref bool noHitbox)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        // Allow the player to open chests by hitting them with melee weapons
        for (var i = 0; i < Main.chest.Length; i++)
        {
            var chest = Main.chest[i];
            if(chest == null) continue;
            var playerSpaceHitbox = new Rectangle(chest.x * 16, chest.y * 16, 32, 32);
            if (playerSpaceHitbox.Intersects(hitbox))
            {
                player.OpenChest(chest.x, chest.y, i);
            }
        }
    }
}