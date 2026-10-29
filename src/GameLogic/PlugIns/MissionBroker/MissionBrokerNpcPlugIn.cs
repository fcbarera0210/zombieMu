// <copyright file="MissionBrokerNpcPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Dedicated NPC talk handler for custom Mission Broker quests.
/// Uses NPC speech + blue messages and double-talk confirmations (no chat commands, no F6 UI).
/// Quests are configured in the admin panel on this NPC's QuestDefinitions.
/// </summary>
[Guid("B7E4C2A1-9F35-4D6E-8B10-2A5C7D9E4F61")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.MissionBrokerNpcPlugIn_Name),
    Description = nameof(PlugInResources.MissionBrokerNpcPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
public class MissionBrokerNpcPlugIn : NpcTalkPlugInBase
{
    private readonly ConcurrentDictionary<Guid, MissionBrokerInteractionState> _states = new();

    /// <inheritdoc />
    public override short DefaultNpcNumber => MissionBrokerConstants.NpcNumber;

    /// <inheritdoc />
    protected override async ValueTask PlayerTalksToConfiguredNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        eventArgs.HasBeenHandled = true;

        if (player.SelectedCharacter is null)
        {
            return;
        }

        var state = this._states.GetOrAdd(player.SelectedCharacter.Id, _ => new MissionBrokerInteractionState());
        var activeState = MissionBrokerQuestService.GetActiveMissionState(player, npc.Definition);

        if (activeState?.ActiveQuest is { } activeQuest)
        {
            await this.HandleActiveMissionAsync(player, npc, state, activeState, activeQuest).ConfigureAwait(false);
            return;
        }

        await this.HandleOfferAsync(player, npc, state).ConfigureAwait(false);
    }

    private async ValueTask HandleActiveMissionAsync(
        Player player,
        NonPlayerCharacter npc,
        MissionBrokerInteractionState state,
        CharacterQuestState activeState,
        QuestDefinition activeQuest)
    {
        var progress = MissionBrokerQuestService.FormatProgress(player, activeState);
        await player.ShowBlueMessageAsync($"[Mission Broker] {progress}").ConfigureAwait(false);

        if (MissionBrokerQuestService.IsReadyToComplete(activeState))
        {
            state.ClearPending();
            if (await MissionBrokerQuestService.CompleteAsync(player, activeQuest).ConfigureAwait(false))
            {
                var name = activeQuest.Name.GetTranslation(player.Culture) ?? activeQuest.Name.ToString() ?? "Mission";
                await MissionBrokerQuestService.SayAsync(player, npc, "Well done. Here is your reward.").ConfigureAwait(false);
                await player.ShowBlueMessageAsync($"[Mission Broker] Completed: {name}").ConfigureAwait(false);
            }
            else
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "I cannot reward you yet. Check your inventory space.").ConfigureAwait(false);
            }

            return;
        }

        if (state is { HasValidPending: true, PendingAction: MissionBrokerPendingAction.Abandon }
            && state.QuestGroup == activeQuest.Group
            && state.QuestNumber == activeQuest.Number)
        {
            state.ClearPending();
            if (await MissionBrokerQuestService.AbandonAsync(player, activeQuest).ConfigureAwait(false))
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "Mission abandoned. Come back when you are ready.").ConfigureAwait(false);
                await player.ShowBlueMessageAsync("[Mission Broker] Mission abandoned.").ConfigureAwait(false);
            }

            return;
        }

        state.SetPending(MissionBrokerPendingAction.Abandon, activeQuest.Group, activeQuest.Number);
        await MissionBrokerQuestService.SayAsync(player, npc, "Keep hunting. Talk to me again to abandon this mission.").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("[Mission Broker] Talk again within a few seconds to ABANDON.").ConfigureAwait(false);
    }

    private async ValueTask HandleOfferAsync(Player player, NonPlayerCharacter npc, MissionBrokerInteractionState state)
    {
        var available = MissionBrokerQuestService.GetAvailableMissions(player, npc.Definition);
        if (available.Count == 0)
        {
            state.ClearPending();
            await MissionBrokerQuestService.SayAsync(player, npc, "I have no missions for you right now.").ConfigureAwait(false);
            await player.ShowBlueMessageAsync("[Mission Broker] No missions available for your level.").ConfigureAwait(false);
            return;
        }

        await player.ShowBlueMessageAsync("[Mission Broker] Available missions:").ConfigureAwait(false);
        for (var i = 0; i < available.Count; i++)
        {
            await player.ShowBlueMessageAsync(FormatMissionSummary(player, available[i], i + 1)).ConfigureAwait(false);
        }

        if (state is { HasValidPending: true, PendingAction: MissionBrokerPendingAction.Accept }
            && available.FirstOrDefault(q => q.Group == state.QuestGroup && q.Number == state.QuestNumber) is { } confirmed)
        {
            state.ClearPending();
            if (await MissionBrokerQuestService.StartAsync(player, confirmed).ConfigureAwait(false))
            {
                var name = confirmed.Name.GetTranslation(player.Culture) ?? confirmed.Name.ToString() ?? "Mission";
                await MissionBrokerQuestService.SayAsync(player, npc, $"Good. Complete '{name}' and return to me.").ConfigureAwait(false);
                await player.ShowBlueMessageAsync($"[Mission Broker] Accepted: {MissionBrokerQuestService.FormatProgress(player, player.GetQuestState(confirmed.Group)!)}").ConfigureAwait(false);
            }
            else
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "You cannot take that mission.").ConfigureAwait(false);
            }

            return;
        }

        state.LastOfferIndex = (state.LastOfferIndex + 1) % available.Count;
        var offer = available[state.LastOfferIndex];
        state.SetPending(MissionBrokerPendingAction.Accept, offer.Group, offer.Number);

        var offerName = offer.Name.GetTranslation(player.Culture) ?? offer.Name.ToString() ?? "Mission";
        await MissionBrokerQuestService.SayAsync(player, npc, $"Offering: {offerName}. Talk again to accept.").ConfigureAwait(false);
        await player.ShowBlueMessageAsync($"[Mission Broker] Offering #{state.LastOfferIndex + 1}: {offerName}. Talk again to ACCEPT. Wait ~12s and talk to see the next mission.").ConfigureAwait(false);
    }

    private static string FormatMissionSummary(Player player, QuestDefinition quest, int index)
    {
        var name = quest.Name.GetTranslation(player.Culture) ?? quest.Name.ToString() ?? "Mission";
        var level = quest.MaximumCharacterLevel > 0
            ? $"Lv {quest.MinimumCharacterLevel}-{quest.MaximumCharacterLevel}"
            : $"Lv {quest.MinimumCharacterLevel}+";
        var repeat = quest.Repeatable ? "repeatable" : "once";
        var kills = string.Join(
            ", ",
            quest.RequiredMonsterKills.Select(r =>
            {
                var monster = r.Monster?.Designation.GetTranslation(player.Culture)
                              ?? r.Monster?.Designation.ToString()
                              ?? "?";
                return $"{r.MinimumNumber}x {monster}";
            }));

        return $"{index}) {name} ({level}, {repeat}) — {kills}";
    }
}
