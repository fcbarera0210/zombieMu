// <copyright file="MissionBrokerQuestService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.MissionBroker;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions.Quests;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// Starts, completes and abandons Mission Broker quests without opening the stock F6 quest UI.
/// Progress still uses <see cref="CharacterQuestState"/> so kill tracking and persistence work as usual.
/// </summary>
internal static class MissionBrokerQuestService
{
    /// <summary>
    /// Gets the active Mission Broker quest for the player, if any.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="npc">The Mission Broker NPC definition.</param>
    /// <returns>The quest state with an active mission, or <c>null</c>.</returns>
    public static CharacterQuestState? GetActiveMissionState(Player player, MonsterDefinition npc)
    {
        return player.SelectedCharacter?.QuestStates
            .FirstOrDefault(state => state.ActiveQuest is { } quest && IsMissionOfNpc(quest, npc));
    }

    /// <summary>
    /// Gets missions the player can accept from this NPC.
    /// Does not require <see cref="Player.OpenedNpc"/> so chat selection still works after the talk dialog closes.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="npc">The NPC.</param>
    /// <returns>The available missions.</returns>
    public static IList<QuestDefinition> GetAvailableMissions(Player player, MonsterDefinition npc)
    {
        var characterClass = player.SelectedCharacter?.CharacterClass;
        return GetQuestsLinkedToNpc(player, npc)
            .Where(quest => quest.QualifiedCharacter is null || Equals(quest.QualifiedCharacter, characterClass))
            .Where(quest => quest.MinimumCharacterLevel <= player.Level
                            && (quest.MaximumCharacterLevel == default || quest.MaximumCharacterLevel >= player.Level))
            .Where(quest => !quest.EvolvesIntoInactiveClass(player))
            .Where(quest => IsMissionOfNpc(quest, npc))
            .Where(quest => CanAccept(player, quest))
            .OrderBy(quest => quest.Number)
            .ToList();
    }

    /// <summary>
    /// Determines whether the active mission meets its kill/item requirements.
    /// </summary>
    /// <param name="questState">The quest state.</param>
    /// <returns><c>true</c> if the mission can be turned in.</returns>
    public static bool IsReadyToComplete(CharacterQuestState questState)
    {
        var activeQuest = questState.ActiveQuest;
        if (activeQuest is null)
        {
            return false;
        }

        foreach (var requiredKills in activeQuest.RequiredMonsterKills)
        {
            var currentKillCount = questState.RequirementStates
                .FirstOrDefault(r => object.Equals(r.Requirement, requiredKills))?.KillCount ?? 0;
            if (currentKillCount < requiredKills.MinimumNumber)
            {
                return false;
            }
        }

        if (activeQuest.RequiresClientAction && !questState.ClientActionPerformed)
        {
            return false;
        }

        return activeQuest.RequiredMonsterKills.Count > 0 || activeQuest.RequiredItems.Count > 0;
    }

    /// <summary>
    /// Builds a short progress line for the active mission.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="questState">The quest state.</param>
    /// <returns>The progress text.</returns>
    public static string FormatProgress(Player player, CharacterQuestState questState)
    {
        var quest = questState.ActiveQuest;
        if (quest is null)
        {
            return string.Empty;
        }

        var name = quest.Name.GetTranslation(player.Culture) ?? quest.Name.ToString() ?? "Mission";
        if (quest.RequiredMonsterKills.Count == 0)
        {
            return $"{name}: in progress";
        }

        var parts = quest.RequiredMonsterKills.Select(requirement =>
        {
            var current = questState.RequirementStates
                .FirstOrDefault(r => object.Equals(r.Requirement, requirement))?.KillCount ?? 0;
            var monsterName = requirement.Monster?.Designation.GetTranslation(player.Culture)
                              ?? requirement.Monster?.Designation.ToString()
                              ?? "Monster";
            return $"{monsterName} {current}/{requirement.MinimumNumber}";
        });

        return $"{name}: {string.Join(", ", parts)}";
    }

    /// <summary>
    /// Starts a mission without sending F6 quest packets.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="quest">The quest.</param>
    /// <returns>A task with a value indicating whether the mission was started.</returns>
    public static async ValueTask<bool> StartAsync(Player player, QuestDefinition quest)
    {
        if (player.SelectedCharacter is null)
        {
            return false;
        }

        if (quest.MinimumCharacterLevel > player.Level
            || (quest.MaximumCharacterLevel > 0 && quest.MaximumCharacterLevel < player.Level))
        {
            return false;
        }

        if (!CanAccept(player, quest))
        {
            return false;
        }

        var questState = player.SelectedCharacter.QuestStates.FirstOrDefault(q => q.Group == quest.Group);
        if (questState is null)
        {
            questState = player.PersistenceContext.CreateNew<CharacterQuestState>();
            questState.Group = quest.Group;
            player.SelectedCharacter.QuestStates.Add(questState);
        }

        if (questState.ActiveQuest != null)
        {
            return false;
        }

        if (quest.RequiredStartMoney > 0)
        {
            if (!player.TryRemoveMoney(quest.RequiredStartMoney))
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnoughMoneyToProceed)).ConfigureAwait(false);
                return false;
            }

            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
        }

        await questState.ClearAsync(player.PersistenceContext).ConfigureAwait(false);
        questState.ActiveQuest = quest;
        return true;
    }

    /// <summary>
    /// Completes the active mission and grants rewards.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="quest">The quest.</param>
    /// <returns>A task with a value indicating whether completion succeeded.</returns>
    public static async ValueTask<bool> CompleteAsync(Player player, QuestDefinition quest)
    {
        var stateBefore = player.GetQuestState(quest.Group);
        if (stateBefore?.ActiveQuest is null || !object.Equals(stateBefore.ActiveQuest, quest))
        {
            return false;
        }

        if (!IsReadyToComplete(stateBefore))
        {
            return false;
        }

        var completion = new QuestCompletionAction();
        await completion.CompleteQuestAsync(player, quest.Group, quest.Number).ConfigureAwait(false);
        return player.GetQuestState(quest.Group)?.ActiveQuest is null;
    }

    /// <summary>
    /// Abandons the active mission without F6 cancel packets.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="quest">The quest.</param>
    /// <returns>A task with a value indicating whether the mission was abandoned.</returns>
    public static async ValueTask<bool> AbandonAsync(Player player, QuestDefinition quest)
    {
        var questState = player.GetQuestState(quest.Group);
        if (questState?.ActiveQuest is null || !object.Equals(questState.ActiveQuest, quest))
        {
            return false;
        }

        await questState.ClearAsync(player.PersistenceContext).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Sends a short NPC speech bubble.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="npc">The NPC.</param>
    /// <param name="message">The message.</param>
    /// <returns>The task.</returns>
    public static ValueTask SayAsync(Player player, NonPlayerCharacter npc, string message)
        => player.InvokeViewPlugInAsync<IShowMessageOfObjectPlugIn>(p => p.ShowMessageOfObjectAsync(message, npc));

    private static bool CanAccept(Player player, QuestDefinition quest)
    {
        var questState = player.GetQuestState(quest.Group);
        if (questState?.ActiveQuest != null)
        {
            return false;
        }

        if (!quest.Repeatable && object.Equals(questState?.LastFinishedQuest, quest))
        {
            return false;
        }

        return true;
    }

    private static IEnumerable<QuestDefinition> GetQuestsLinkedToNpc(Player player, MonsterDefinition npc)
    {
        return npc.Quests
            .Concat(player.GameContext.Configuration.Monsters.SelectMany(monster => monster.Quests)
                .Where(quest => IsQuestGiver(quest, npc)))
            .Distinct();
    }

    private static bool IsMissionOfNpc(QuestDefinition quest, MonsterDefinition npc)
    {
        if (npc.Quests.Contains(quest))
        {
            return true;
        }

        return IsQuestGiver(quest, npc);
    }

    private static bool IsQuestGiver(QuestDefinition quest, MonsterDefinition npc)
    {
        if (quest.QuestGiver is null)
        {
            return false;
        }

        return ReferenceEquals(quest.QuestGiver, npc) || quest.QuestGiver.Number == npc.Number;
    }
}
