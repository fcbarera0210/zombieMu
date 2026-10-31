// <copyright file="MissionBrokerNpcPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Dedicated NPC talk handler for custom Mission Broker quests.
/// Talk shows actives + numbered offers; the player types a number / ABANDON [N] in normal chat.
/// Multiple missions may be active at once in group <see cref="MissionBrokerConstants.QuestGroup"/>.
/// </summary>
[Guid("B7E4C2A1-9F35-4D6E-8B10-2A5C7D9E4F61")]
[PlugIn]
[Display(
    Name = nameof(PlugInResources.MissionBrokerNpcPlugIn_Name),
    Description = nameof(PlugInResources.MissionBrokerNpcPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
public class MissionBrokerNpcPlugIn : NpcTalkPlugInBase, IChatMessageReceivedPlugIn
{
    private static readonly Regex AbandonPattern = new(
        @"^\s*abandon(?:\s+(\d+))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
        var abandonMatch = AbandonPattern.Match(trimmed);
        if (abandonMatch.Success)
        {
            cancelEventArgs.Cancel = true;
            _ = this.HandleAbandonChatAsync(sender, state, abandonMatch.Groups[1].Value);
            return;
        }

        if (state.MenuKind == MissionBrokerMenuKind.Selection
            && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            cancelEventArgs.Cancel = true;
            if (index < 1 || index > state.OfferedMissions.Count)
            {
                _ = sender.ShowGoldenMessageAsync(
                    state.OfferedMissions.Count == 0
                        ? "[Mission Broker] No missions to accept right now. Type ABANDON [N] to quit one."
                        : $"[Mission Broker] Invalid number. Choose 1-{state.OfferedMissions.Count}.");
                return;
            }

            var quest = state.OfferedMissions[index - 1];
            state.ClearMenu();
            _ = this.AcceptOfferAsync(sender, quest);
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
        var actives = MissionBrokerQuestService.GetActiveMissionStates(player, npc.Definition);
        var ready = actives.Where(MissionBrokerQuestService.IsReadyToComplete).ToList();

        if (ready.Count > 0)
        {
            state.ClearMenu();
            await this.CompleteReadyMissionsAsync(player, npc, ready).ConfigureAwait(false);

            // Refresh list after turn-ins so the player can keep accepting / abandoning.
            actives = MissionBrokerQuestService.GetActiveMissionStates(player, npc.Definition);
        }

        await this.OpenSelectionAsync(player, npc, state, actives).ConfigureAwait(false);
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

    private async ValueTask CompleteReadyMissionsAsync(Player player, NonPlayerCharacter npc, IList<CharacterQuestState> ready)
    {
        var completed = 0;
        foreach (var readyState in ready)
        {
            var quest = readyState.ActiveQuest;
            if (quest is null)
            {
                continue;
            }

            if (await MissionBrokerQuestService.CompleteAsync(player, quest).ConfigureAwait(false))
            {
                completed++;
                var name = GetQuestName(player, quest);
                await player.ShowGoldenMessageAsync($"[Mission Broker] Completed: {name}").ConfigureAwait(false);
            }
            else
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "I cannot reward you yet. Check your inventory space.").ConfigureAwait(false);
                await player.ShowBlueMessageAsync("[Mission Broker] Free inventory space and talk again.").ConfigureAwait(false);
                return;
            }
        }

        if (completed > 0)
        {
            await MissionBrokerQuestService.SayAsync(player, npc, "Well done. Here is your reward.").ConfigureAwait(false);
        }
    }

    private async ValueTask OpenSelectionAsync(
        Player player,
        NonPlayerCharacter npc,
        MissionBrokerInteractionState state,
        IList<CharacterQuestState> actives)
    {
        var available = MissionBrokerQuestService.GetAvailableMissions(player, npc.Definition);
        if (actives.Count == 0 && available.Count == 0)
        {
            state.ClearMenu();
            await MissionBrokerQuestService.SayAsync(player, npc, "I have no missions for you right now.").ConfigureAwait(false);
            await player.ShowGoldenMessageAsync("[Mission Broker] No missions available for your level.").ConfigureAwait(false);
            return;
        }

        state.OpenSelection(available, actives);
        await MissionBrokerQuestService.SayAsync(player, npc, "Check chat for missions. Type a number to accept, or ABANDON [N].").ConfigureAwait(false);

        if (actives.Count > 0)
        {
            await player.ShowGoldenMessageAsync(
                $"[Mission Broker] Active missions ({actives.Count}/{MissionBrokerConstants.MaxActiveMissions}):").ConfigureAwait(false);
            for (var i = 0; i < actives.Count; i++)
            {
                var progress = MissionBrokerQuestService.FormatProgress(player, actives[i]);
                await player.ShowBlueMessageAsync($"  A{i + 1}) {progress}").ConfigureAwait(false);
            }

            await player.ShowBlueMessageAsync(
                actives.Count == 1
                    ? "[Mission Broker] Type ABANDON in chat within 60s to quit the active mission."
                    : "[Mission Broker] Type ABANDON N in chat within 60s (N = A1, A2, …).").ConfigureAwait(false);
        }

        if (available.Count == 0)
        {
            if (actives.Count >= MissionBrokerConstants.MaxActiveMissions)
            {
                await player.ShowGoldenMessageAsync(
                    $"[Mission Broker] Active limit reached ({MissionBrokerConstants.MaxActiveMissions}). Finish or abandon one first.")
                    .ConfigureAwait(false);
            }

            return;
        }

        if (actives.Count >= MissionBrokerConstants.MaxActiveMissions)
        {
            await player.ShowGoldenMessageAsync(
                $"[Mission Broker] Active limit reached ({MissionBrokerConstants.MaxActiveMissions}). Finish or abandon one first.")
                .ConfigureAwait(false);
            return;
        }

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
            var npcDefinition = npc?.Definition
                                ?? this.Configuration?.Npc
                                ?? player.GameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == this.DefaultNpcNumber);
            if (npcDefinition is null)
            {
                await player.ShowGoldenMessageAsync("[Mission Broker] That mission cannot be accepted.").ConfigureAwait(false);
                return;
            }

            if (await MissionBrokerQuestService.StartAsync(player, quest, npcDefinition).ConfigureAwait(false))
            {
                var progressState = MissionBrokerQuestService.GetMissionState(player, quest);
                var progress = progressState is null
                    ? GetQuestName(player, quest)
                    : MissionBrokerQuestService.FormatProgress(player, progressState);
                if (npc is not null)
                {
                    await MissionBrokerQuestService.SayAsync(player, npc, $"Good. Complete '{GetQuestName(player, quest)}' and return.").ConfigureAwait(false);
                }

                await player.ShowGoldenMessageAsync($"[Mission Broker] Accepted: {progress}").ConfigureAwait(false);

                foreach (var hint in MissionBrokerSpawnHint.FormatHuntHints(player, quest))
                {
                    await player.ShowBlueMessageAsync($"[Mission Broker] {hint}").ConfigureAwait(false);
                }
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

    private async Task HandleAbandonChatAsync(Player player, MissionBrokerInteractionState state, string numberText)
    {
        try
        {
            var actives = state.ActiveMissions
                .Where(active => active.ActiveQuest is not null)
                .ToList();
            if (actives.Count == 0)
            {
                // Menu may be stale; re-resolve from character.
                var npcDefinition = this.Configuration?.Npc
                                     ?? player.GameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == this.DefaultNpcNumber);
                if (npcDefinition is not null)
                {
                    actives = MissionBrokerQuestService.GetActiveMissionStates(player, npcDefinition).ToList();
                }
            }

            if (actives.Count == 0)
            {
                state.ClearMenu();
                await player.ShowGoldenMessageAsync("[Mission Broker] You have no active mission.").ConfigureAwait(false);
                return;
            }

            CharacterQuestState? target = null;
            if (string.IsNullOrEmpty(numberText))
            {
                if (actives.Count == 1)
                {
                    target = actives[0];
                }
                else
                {
                    await player.ShowGoldenMessageAsync(
                        $"[Mission Broker] Several active missions — type ABANDON 1-{actives.Count}.").ConfigureAwait(false);
                    return;
                }
            }
            else if (int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                     && index >= 1
                     && index <= actives.Count)
            {
                target = actives[index - 1];
            }

            if (target?.ActiveQuest is not { } activeQuest)
            {
                await player.ShowGoldenMessageAsync(
                    $"[Mission Broker] Invalid abandon target. Type ABANDON 1-{actives.Count}.").ConfigureAwait(false);
                return;
            }

            state.ClearMenu();
            await this.AbandonMissionAsync(player, activeQuest).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Mission Broker abandon failed for {Player}", player.SelectedCharacter?.Name);
        }
    }

    private async Task AbandonMissionAsync(Player player, QuestDefinition activeQuest)
    {
        var npc = FindNearbyMissionBroker(player);
        if (await MissionBrokerQuestService.AbandonAsync(player, activeQuest).ConfigureAwait(false))
        {
            if (npc is not null)
            {
                await MissionBrokerQuestService.SayAsync(player, npc, "Mission abandoned. Come back when you are ready.").ConfigureAwait(false);
            }

            await player.ShowGoldenMessageAsync(
                $"[Mission Broker] Abandoned: {GetQuestName(player, activeQuest)}.").ConfigureAwait(false);
        }
        else
        {
            await player.ShowGoldenMessageAsync("[Mission Broker] You have no active mission.").ConfigureAwait(false);
        }
    }
}
