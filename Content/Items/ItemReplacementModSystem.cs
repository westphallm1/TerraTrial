using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using TerraTrial.Content.Players;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Content.Items;

/// <summary>
/// Replace any items that would appear in the world with power up patches.
/// </summary>
public class ItemReplacementModSystem : ModSystem
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

/// <summary>
/// Allow the player to open chests by hitting them with melee weapons 
/// </summary>
public class ChestOpenGlobalItem : GlobalItem
{
    public override void UseItemHitbox(Item item, Player player, ref Rectangle hitbox, ref bool noHitbox)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
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

public class ItemDropGlobalNpc : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (!SubworldSystem.IsActive<TerraTrialWorld>()) return;
        
        var itemType = ItemPatch.PatchIDs[Main._rand.Next(ItemPatch.PatchIDs.Count)];
        var itemIdx = Item.NewItem(npc.GetSource_FromThis(), npc.Center, itemType);
        
        // Kick the item away from the player that killed the NPC
        var playerIdx = npc.lastInteraction;
        if (!Main.player[playerIdx].active || Main.player[playerIdx].dead)
        {
            playerIdx = npc.FindClosestPlayer();
        }

        var player = Main.player[playerIdx];

        var offsetToPlayer = (npc.Center - player.Center).SafeNormalize(default);
        offsetToPlayer *= Main.rand.Next(20, 40) / 10f;

        Main.item[itemIdx].velocity = offsetToPlayer;
    }
}