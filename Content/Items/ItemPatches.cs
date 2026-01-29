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
        ModContent.ItemType<AgilityPatch>(),
        ModContent.ItemType<FlightPatch>(),
        ModContent.ItemType<HeartPatch>(),
        ModContent.ItemType<AttackPatch>(),
        ModContent.ItemType<DefensePatch>(),
        ModContent.ItemType<WeightPatch>(),
    ];
}

public class SpeedPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.HermesBoots;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Speed += 1;
}

public class AgilityPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.FeralClaws;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Acceleration += 1;
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

public class DefensePatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.CobaltShield;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Defense += 1;
}

public class WeightPatch : ItemPatch
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.IronAnvil;
    public override void ModifyPlayer(TerraTrialPlayer player) => player.Weight += 1;
}
