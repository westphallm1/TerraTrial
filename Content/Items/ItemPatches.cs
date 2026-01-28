using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraTrial.Content.Players;

namespace TerraTrial.Content.Items;

public abstract class ItemPatch : ModItem
{
    public abstract void ModifyPlayer(TerraTrialPlayer player);

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.Heart);
    }

    public override bool OnPickup(Player player)
    {
        ModifyPlayer(player.GetModPlayer<TerraTrialPlayer>());
        return false;
    }

    public override bool CanPickup(Player player)
    {
        return Item.timeSinceItemSpawned >= 60;
    }

    public override void Update(ref float gravity, ref float maxFallSpeed)
    {
        // Don't fall, but gradually come to a stop in the air
        gravity = 0;
        maxFallSpeed = 0;
        if (Item.timeSinceItemSpawned > 15)
        {
            Item.velocity *= 0.5f;
        }
    }

    public static List<int> PatchIDs =>
    [
        ModContent.ItemType<SpeedPatch>(),
        ModContent.ItemType<FlightPatch>(),
        ModContent.ItemType<HeartPatch>(),
        ModContent.ItemType<AttackPatch>(),
    ];
}

public class SpeedPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.HermesBoots;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Speed += 1;
}

public class FlightPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.AngelWings;
    
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Jump += 1;
}

public class HeartPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.LifeCrystal;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Health += 1;
}

public class AttackPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.FireGauntlet;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Attack += 1;
}
