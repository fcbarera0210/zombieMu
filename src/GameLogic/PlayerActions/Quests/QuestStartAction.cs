// <copyright file="QuestStartAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Quest;

/// <summary>
/// A player action which implements the starting of a quest.
/// </summary>
public class QuestStartAction
{
    /// <summary>
    /// Tries to start the quest of the given group and number for the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The group.</param>
    /// <param name="number">The number.</param>
    public async ValueTask StartQuestAsync(Player player, short group, short number)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        var quest = player.GetQuest(group, number);
        if (quest is null)
        {
            player.Logger.LogWarning(
                "Quest start failed: no quest for group {Group} number {Number} (OpenedNpc={Npc}).",
                group,
                number,
                player.OpenedNpc?.Definition.Number);
            await player.ShowGoldenMessageAsync(
                    "[Quest] This mission cannot be started (not found for this NPC).")
                .ConfigureAwait(false);
            return;
        }

        var questName = quest.Name.GetTranslation(player.Culture) ?? quest.Name.ToString() ?? "Quest";

        var questState = player.SelectedCharacter!.QuestStates.FirstOrDefault(q => q.Group == group);
        if (questState is null)
        {
            questState = player.PersistenceContext.CreateNew<CharacterQuestState>();
            questState.Group = group;
            player.SelectedCharacter.QuestStates.Add(questState);
        }

        // Already active: always show progress (turn-in), even if the player outleveled the start range.
        if (questState.ActiveQuest != null)
        {
            player.Logger.LogDebug("There is already an active quest of this group.");
            await player.InvokeViewPlugInAsync<IQuestProgressPlugIn>(p => p.ShowQuestProgressAsync(questState.ActiveQuest, false)).ConfigureAwait(false);
            return;
        }

        if (!player.IsWithinQuestLevelRange(quest))
        {
            var maxText = quest.MaximumCharacterLevel > 0
                ? quest.MaximumCharacterLevel.ToString()
                : "∞";
            player.Logger.LogInformation(
                "Quest start failed level range: {QuestName}, player {Level}, allowed {Min}-{Max}.",
                questName,
                player.Level,
                quest.MinimumCharacterLevel,
                quest.MaximumCharacterLevel);
            await player.ShowGoldenMessageAsync(
                    $"[Quest] '{questName}' requires level {quest.MinimumCharacterLevel}-{maxText} (you are {player.Level}).")
                .ConfigureAwait(false);
            return;
        }

        if (MissionBrokerConstants.IsMissionBrokerGroup(quest.Group)
            && !MissionBrokerQuestService.CanAcceptAnother(player))
        {
            player.Logger.LogDebug(
                "Failed, Mission Broker active limit reached ({0}).",
                MissionBrokerConstants.MaxActiveMissions);
            await player.ShowGoldenMessageAsync(
                    $"[Mission Broker] Active limit reached ({MissionBrokerConstants.MaxActiveMissions}). Finish or give up one first.")
                .ConfigureAwait(false);
            await player.ShowBlueMessageAsync(
                    $"[Mission Broker] You can have at most {MissionBrokerConstants.MaxActiveMissions} missions at once.")
                .ConfigureAwait(false);
            return;
        }

        if (quest.IsBlockedByCompletionHistory(questState.LastFinishedQuest))
        {
            player.Logger.LogInformation(
                "Quest start blocked by history: {QuestName} (G{Group}/N{Number}).",
                questName,
                quest.Group,
                quest.Number);
            await player.ShowGoldenMessageAsync(
                    $"[Quest] '{questName}' was already completed and cannot be taken again.")
                .ConfigureAwait(false);
            return;
        }

        if (quest.RequiredStartMoney > 0)
        {
            if (player.TryRemoveMoney(quest.RequiredStartMoney))
            {
                await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
            }
            else
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnoughMoneyToProceed)).ConfigureAwait(false);
                return;
            }
        }

        await questState.ClearAsync(player.PersistenceContext).ConfigureAwait(false);
        questState.ActiveQuest = quest;
        await player.InvokeViewPlugInAsync<IQuestStartedPlugIn>(p => p.QuestStartedAsync(quest)).ConfigureAwait(false);
    }
}