// <copyright file="MissionBrokerNpcPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Dedicated NPC talk handler for custom Mission Broker quests.
/// Talk shows a numbered list (or progress); the player types the number / ABANDON in normal chat.
/// Quests are configured in the admin panel on this NPC's QuestDefinitions.
/// </summary>
[Guid("B7E4C2A1-9F35-4D6E-8B10-2A5C7D9E4F61")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.MissionBrokerNpcPlugIn_Name),
    Description = nameof(PlugInResources.MissionBrokerNpcPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
public class MissionBrokerNpcPlugIn : NpcTalkPlugInBase, IChatMessageReceivedPlugIn
{
    /// <summary>
    /// Shared across plugin-point instances so talk and chat handlers see the same menu state.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, MissionBrokerInteractionState> States = new();

    /// <inheritdoc />
    public override short DefaultNpcNumber => MissionBrokerConstants.NpcNumber;

    /// <inheritdoc />
    public void ChatMessageReceived(Player sender, string message, CancelEventArgs cancelEventArgs)
    {
        if (sender.SelectedCharacter is null
            || !States.TryGetValue(sender.SelectedCharacter.Id, out var state)
            || !state.HasOpenMenu)
        {
            return;
        }

        var trimmed = message.Trim();
        if (state.MenuKind == MissionBrokerMenuKind.Offers
            && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            cancelEventArgs.Cancel = true;
            if (index < 1 || index > state.OfferedMissions.Count)
            {
                _ = sender.ShowGoldenMessageAsync($"[Mission Broker] Invalid number. Choose 1-{state.OfferedMissions.Count}.");
                return;
            }

            var quest = state.OfferedMissions[index - 1];
            state.ClearMenu();
            _ = this.AcceptOfferAsync(sender, quest);
            return;
        }

        if (state.MenuKind == MissionBrokerMenuKind.Abandon
            && trimmed.Equals(MissionBrokerConstants.AbandonKeyword, StringComparison.OrdinalIgnoreCase))
        {
            cancelEventArgs.Cancel = true;
            state.ClearMenu();
            _ = this.AbandonActiveMissionAsync(sender);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask PlayerTalksToConfiguredNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        eventArgs.HasBeenHandled = true;

        if (player.SelectedCharacter is null)
        {
            return;
        }

        var state = States.GetOrAdd(player.SelectedCharacter.Id, _ => new MissionBrokerInteractionState());
        var activeState = MissionBrokerQuestService.GetActiveMissionState(player, npc.Definition);

        if (activeState?.ActiveQuest is { } activeQuest)
        {
            await this.HandleActiveMissionAsync(player, npc, state, activeState, activeQuest).ConfigureAwait(false);
            return;
        }

        await this.HandleOfferAsync(player, npc, state).ConfigureAwait(false);
    }

    private static NonPlayerCharacter? FindNearbyMissionBroker(Player player)
    {
        if (player.CurrentMap is null || player.SelectedCharacter is null)
        {
            return null;
        }

        return player.CurrentMap
            .GetNpcsInRange(player.Position, 10)
            .FirstOrDefault(npc => npc.Definition.Number == MissionBrokerConstants.NpcNumber);
    }

    private static string GetQuestName(Player player, QuestDefinition quest)
        => quest.Name.GetTranslation(player.Culture) ?? quest.Name.ToString() ?? "Mission";

    private static string FormatMissionSummary(Player player, QuestDefinition quest, int index)
    {
        var name = GetQuestName(player, quest);
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

    private async ValueTask HandleActiveMissionAsync(
        Player player,
        NonPlayerCharacter npc,
        MissionBrokerInteractionState state,
        CharacterQuestState activeState,
        QuestDefinition activeQuest)
    {
        var progress = MissionBrokerQuestService.FormatProgress(player, activeState);

        if (MissionBrokerQuestService.IsReadyToComplete(activeState))
        {
            state.ClearMenu();
            if (await MissionBrokerQuestService.CompleteAsync(player, activeQuest).ConfigureAwait(false))
            {
                var name = GetQuestName(player, activeQuest);
                await MissionBrokerQuestService.SayAsync(player, npc, "Well done. Here is your reward.").ConfigureAwait(false);
                await player.ShowGoldenMessageAsync($"[Mission Broker] Completed: {name}").ConfigureAwait(false);
            }
            else
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "I cannot reward you yet. Check your inventory space.").ConfigureAwait(false);
                await player.ShowBlueMessageAsync("[Mission Broker] Free inventory space and talk again.").ConfigureAwait(false);
            }

            return;
        }

        state.OpenAbandonPrompt();
        await MissionBrokerQuestService.SayAsync(player, npc, "Keep hunting. Type ABANDON in chat to quit.").ConfigureAwait(false);
        await player.ShowGoldenMessageAsync($"[Mission Broker] {progress}").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("[Mission Broker] Type ABANDON in chat within 60s to abandon this mission.").ConfigureAwait(false);
    }

    private async ValueTask HandleOfferAsync(Player player, NonPlayerCharacter npc, MissionBrokerInteractionState state)
    {
        var available = MissionBrokerQuestService.GetAvailableMissions(player, npc.Definition);
        if (available.Count == 0)
        {
            state.ClearMenu();
            await MissionBrokerQuestService.SayAsync(player, npc, "I have no missions for you right now.").ConfigureAwait(false);
            await player.ShowGoldenMessageAsync("[Mission Broker] No missions available for your level.").ConfigureAwait(false);
            return;
        }

        state.OpenOffers(available);
        await MissionBrokerQuestService.SayAsync(player, npc, "Type the mission number in chat to accept.").ConfigureAwait(false);
        await player.ShowGoldenMessageAsync("[Mission Broker] Type a number in chat to accept (e.g. 1).").ConfigureAwait(false);
        await player.ShowBlueMessageAsync("[Mission Broker] Available missions:").ConfigureAwait(false);
        for (var i = 0; i < available.Count; i++)
        {
            await player.ShowBlueMessageAsync(FormatMissionSummary(player, available[i], i + 1)).ConfigureAwait(false);
        }
    }

    private async Task AcceptOfferAsync(Player player, QuestDefinition quest)
    {
        try
        {
            var npc = FindNearbyMissionBroker(player);
            if (await MissionBrokerQuestService.StartAsync(player, quest).ConfigureAwait(false))
            {
                var progressState = player.GetQuestState(quest.Group);
                var progress = progressState is null
                    ? GetQuestName(player, quest)
                    : MissionBrokerQuestService.FormatProgress(player, progressState);
                if (npc is not null)
                {
                    await MissionBrokerQuestService.SayAsync(player, npc, $"Good. Complete '{GetQuestName(player, quest)}' and return.").ConfigureAwait(false);
                }

                await player.ShowGoldenMessageAsync($"[Mission Broker] Accepted: {progress}").ConfigureAwait(false);
            }
            else
            {
                if (npc is not null)
                {
                    await MissionBrokerQuestService.SayAsync(player, npc, "You cannot take that mission.").ConfigureAwait(false);
                }

                await player.ShowGoldenMessageAsync("[Mission Broker] That mission cannot be accepted.").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Mission Broker accept failed for {Player}", player.SelectedCharacter?.Name);
        }
    }

    private async Task AbandonActiveMissionAsync(Player player)
    {
        try
        {
            var npcDefinition = this.Configuration?.Npc
                                 ?? player.GameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == this.DefaultNpcNumber);
            if (npcDefinition is null)
            {
                return;
            }

            var activeState = MissionBrokerQuestService.GetActiveMissionState(player, npcDefinition);
            if (activeState?.ActiveQuest is not { } activeQuest)
            {
                await player.ShowGoldenMessageAsync("[Mission Broker] You have no active mission.").ConfigureAwait(false);
                return;
            }

            var npc = FindNearbyMissionBroker(player);
            if (await MissionBrokerQuestService.AbandonAsync(player, activeQuest).ConfigureAwait(false))
            {
                if (npc is not null)
                {
                    await MissionBrokerQuestService.SayAsync(player, npc, "Mission abandoned. Come back when you are ready.").ConfigureAwait(false);
                }

                await player.ShowGoldenMessageAsync("[Mission Broker] Mission abandoned.").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Mission Broker abandon failed for {Player}", player.SelectedCharacter?.Name);
        }
    }
}
