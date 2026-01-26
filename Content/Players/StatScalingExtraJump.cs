using Terraria;
using Terraria.ModLoader;

namespace TerraTrial.Content.Players;

public class StatScalingExtraJump : ExtraJump
{
    public override Position GetDefaultPosition() => new After(BlizzardInABottle);

    public override float GetDurationMultiplier(Player player) =>
        0.75f + player.GetModPlayer<TerraTrialPlayer>().Jump / 10f;
}