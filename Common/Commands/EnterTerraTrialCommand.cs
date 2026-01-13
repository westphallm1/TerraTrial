using SubworldLibrary;
using Terraria.ModLoader;
using TerraTrial.Content.Subworlds;

namespace TerraTrial.Common.Commands;

public class EnterTerraTrialCommand : ModCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        SubworldSystem.Enter<TerraTrialWorld>();
    }

    public override string Command => "enter";
    public override CommandType Type => CommandType.Chat;
}