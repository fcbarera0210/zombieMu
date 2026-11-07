// <copyright file="QuestExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Extensions regarding quests.
/// </summary>
public static class QuestExtensions
{
    /// <summary>
    /// Clears the quest state for the specified player.
    /// </summary>
    /// <param name="questState">State of the quest.</param>
    /// <param name="persistenceContext">The persistence context of the player.</param>
    public static async ValueTask ClearAsync(this CharacterQuestState questState, IContext persistenceContext)
    {
        questState.ActiveQuest = null;
        questState.ClientActionPerformed = false;
        foreach (var requirementState in questState.RequirementStates)
        {
            await persistenceContext.DeleteAsync(requirementState).ConfigureAwait(false);
        }

        questState.RequirementStates.Clear();
    }

    /// <summary>
    /// Determines whether the quest cannot be accepted again because of its completion history.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <param name="lastFinishedQuest">The last finished quest of the matching group, if any.</param>
    /// <returns>
    /// <c>true</c> when the character already finished this quest and it is either not
    /// <see cref="QuestDefinition.Repeatable"/> or marked as <see cref="QuestDefinition.OncePerReset"/>.
    /// </returns>
    public static bool IsBlockedByCompletionHistory(this QuestDefinition quest, QuestDefinition? lastFinishedQuest)
    {
        if (!Equals(lastFinishedQuest, quest))
        {
            return false;
        }

        return !quest.Repeatable || quest.OncePerReset;
    }

    /// <summary>
    /// Determines whether the character class may accept the quest based on allowed/disallowed lists.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <param name="characterClass">The character class.</param>
    /// <returns><c>true</c> when the class is allowed.</returns>
    public static bool IsAllowedForCharacterClass(this QuestDefinition quest, CharacterClass? characterClass)
    {
        if (characterClass is null)
        {
            return false;
        }

        if (quest.DisallowedCharacters is { Count: > 0 }
            && quest.DisallowedCharacters.Any(c => Equals(c, characterClass)))
        {
            return false;
        }

        if (quest.QualifiedCharacters is not { Count: > 0 })
        {
            return true;
        }

        return quest.QualifiedCharacters.Any(c => Equals(c, characterClass));
    }

    /// <summary>
    /// Gets the rewards the player must choose from (exactly one) on completion.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <returns>The choice-option rewards.</returns>
    public static IList<QuestReward> GetChoiceRewards(this QuestDefinition quest)
        => quest.Rewards.Where(reward => reward.IsChoiceOption).ToList();

    /// <summary>
    /// Clears finish history of quests marked as <see cref="QuestDefinition.OncePerReset"/> so they can be taken again after a character reset.
    /// </summary>
    /// <param name="character">The character that just reset.</param>
    public static void ClearOncePerResetQuestHistory(this Character character)
    {
        if (character.QuestStates is not { Count: > 0 } questStates)
        {
            return;
        }

        foreach (var questState in questStates)
        {
            if (questState.LastFinishedQuest?.OncePerReset == true)
            {
                questState.LastFinishedQuest = null;
            }
        }
    }
}