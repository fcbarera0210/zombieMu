// <copyright file="MissionsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Chat command for players to list their active Mission Broker missions and progress.
/// </summary>
[Guid("A3C8E1F2-4B7D-4A9E-8C1F-6D2E9B0A5F47")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.MissionsChatCommandPlugIn_Name),
    Description = nameof(PlugInResources.MissionsChatCommandPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(EmptyChatCommandArgs), CharacterStatus.Normal)]
public class MissionsChatCommandPlugIn : ChatCommandPlugInBase<EmptyChatCommandArgs>
{
    private const string Command = "/missions";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, EmptyChatCommandArgs arguments)
    {
        var actives = MissionBrokerQuestService.GetActiveMissionStates(player);
        if (actives.Count == 0)
        {
            await MissionBrokerQuestService.ShowChatAsync(
                player,
                "[Mission Broker] No active missions. Talk to the Mission Broker to accept one.")
                .ConfigureAwait(false);
            await player.ShowGoldenMessageAsync("[Mission Broker] No active missions.").ConfigureAwait(false);
            return;
        }

        await player.ShowGoldenMessageAsync(
            $"[Mission Broker] Active missions ({actives.Count}/{MissionBrokerConstants.MaxActiveMissions})")
            .ConfigureAwait(false);
        await MissionBrokerQuestService.ShowChatAsync(
            player,
            $"[Mission Broker] Active missions ({actives.Count}/{MissionBrokerConstants.MaxActiveMissions}):")
            .ConfigureAwait(false);

        for (var i = 0; i < actives.Count; i++)
        {
            var state = actives[i];
            var progress = MissionBrokerQuestService.FormatProgress(player, state);
            var ready = MissionBrokerQuestService.IsReadyToComplete(state) ? " — READY" : string.Empty;
            await MissionBrokerQuestService.ShowChatAsync(player, $"  A{i + 1}) {progress}{ready}").ConfigureAwait(false);

            if (state.ActiveQuest is { } quest)
            {
                foreach (var hint in MissionBrokerSpawnHint.FormatHuntHints(player, quest).Take(2))
                {
                    await MissionBrokerQuestService.ShowChatAsync(player, $"     {hint}").ConfigureAwait(false);
                }
            }
        }

        await MissionBrokerQuestService.ShowChatAsync(
            player,
            "[Mission Broker] Talk to the Mission Broker to turn in or abandon (ABANDON [N]).")
            .ConfigureAwait(false);
    }
}
