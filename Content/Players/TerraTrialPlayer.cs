using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.Players;

public class TerraTrialPlayer : ModPlayer
{
    internal int Speed { get; set; } = 1;
    
    internal int Jump { get; set; } = 1;

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
            health.Base = 25;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        Player.moveSpeed += 0.1f + Speed / 5f;
        Player.jumpSpeedBoost += Jump;
        Player.autoReuseAllWeapons = true;
    }

    public override void PostUpdateRunSpeeds()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        base.PostUpdateRunSpeeds();
        Player.runAcceleration *= 1.75f;
        Player.runSlowdown *= 1.75f;
        Player.maxRunSpeed *= 1.15f;
        Player.accRunSpeed *= 1.15f;
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
        var player = self.GetModPlayer<TerraTrialPlayer>();
        player.Jump += 1;
        player.Speed += 1;
        Main.NewText($"Player stats updated! Speed: {player.Speed}, Jump {player.Jump}");
    }

    public override void PreUpdateItems()
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        foreach (var item in Main.item)
        {
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