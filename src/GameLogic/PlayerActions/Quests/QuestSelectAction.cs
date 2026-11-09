// <copyright file="QuestSelectAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.Views.Quest;

/// <summary>
/// A player action which implements the selecting of a quest in the quest list.
/// </summary>
public class QuestSelectAction
{
    /// <summary>
    /// Tries to select the quest of the given group and number for the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The <see cref="QuestDefinition.Group"/>.</param>
    /// <param name="number">The <see cref="QuestDefinition.StartingNumber"/>.</param>
    public async ValueTask SelectQuestAsync(Player player, short group, short number)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        var quest = player.GetQuest(group, number);
        if (quest is null)
        {
            player.Logger.LogWarning(
                "Quest select failed: no quest for group {Group} number {Number} (OpenedNpc={Npc}, class={Class}, level={Level}).",
                group,
                number,
                player.OpenedNpc?.Definition.Number,
                player.SelectedCharacter?.CharacterClass?.Name,
                player.Level);
            await player.ShowGoldenMessageAsync(
                    "[Quest] This mission is not available (missing definition, wrong NPC link, or class filter).")
                .ConfigureAwait(false);
            return;
        }

        var questName = quest.Name.GetTranslation(player.Culture) ?? quest.Name.ToString() ?? "Quest";
        var questState = player.SelectedCharacter?.QuestStates.FirstOrDefault(q => q.Group == group);
        if (questState?.ActiveQuest != null)
        {
            player.Logger.LogDebug("There is already an active quest of this group.");
            await player.InvokeViewPlugInAsync<IQuestProgressPlugIn>(p => p.ShowQuestProgressAsync(questState.ActiveQuest, false)).ConfigureAwait(false);
            return;
        }

        if (quest.IsBlockedByCompletionHistory(questState?.LastFinishedQuest))
        {
            player.Logger.LogInformation(
                "Quest select blocked by history: {QuestName} (G{Group}/S{Starting}/N{Number}), Repeatable={Repeatable}, OncePerReset={OncePerReset}.",
                questName,
                quest.Group,
                quest.StartingNumber,
                quest.Number,
                quest.Repeatable,
                quest.OncePerReset);
            await player.ShowGoldenMessageAsync(
                    $"[Quest] '{questName}' was already completed and cannot be taken again.")
                .ConfigureAwait(false);
            await player.ShowBlueMessageAsync(
                    "[Quest] This mission is one-time only for this character.")
                .ConfigureAwait(false);
            return;
        }

        // Level range only gates starting a new quest; active missions are handled above.
        if (!player.IsWithinQuestLevelRange(quest))
        {
            var maxText = quest.MaximumCharacterLevel > 0
                ? quest.MaximumCharacterLevel.ToString()
                : "∞";
            player.Logger.LogInformation(
                "Quest select failed level range: {QuestName}, player {Level}, allowed {Min}-{Max}.",
                questName,
                player.Level,
                quest.MinimumCharacterLevel,
                quest.MaximumCharacterLevel);
            await player.ShowGoldenMessageAsync(
                    $"[Quest] '{questName}' requires level {quest.MinimumCharacterLevel}-{maxText} (you are {player.Level}).")
                .ConfigureAwait(false);
            return;
        }

        if (quest.StartingNumber == quest.Number)
        {
            player.Logger.LogWarning(
                "Quest {QuestName} has StartingNumber == Number ({Number}). Accept UI may loop; configure distinct values.",
                questName,
                quest.Number);
        }

        // Always send starting-step info for selectable quests, including simple missions
        // where StartingNumber == Number (Mission Broker and similar).
        if (quest.StartingNumber == number || quest.Number == number)
        {
            await player.InvokeViewPlugInAsync<IQuestStepInfoPlugIn>(p => p.ShowQuestStepInfoAsync(quest.Group, quest.StartingNumber)).ConfigureAwait(false);
        }
    }
}
