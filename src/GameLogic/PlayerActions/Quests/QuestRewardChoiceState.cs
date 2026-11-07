// <copyright file="QuestRewardChoiceState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Quests;

using System.Collections.Concurrent;
using MUnique.OpenMU.DataModel.Configuration.Quests;

/// <summary>
/// Tracks pending reward-choice menus opened when a quest with <see cref="QuestReward.IsChoiceOption"/>
/// rewards is ready to complete.
/// </summary>
internal static class QuestRewardChoiceState
{
    private static readonly ConcurrentDictionary<Guid, PendingChoice> Pending = new();

    /// <summary>
    /// Opens a pending choice for the character.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="quest">The quest being completed.</param>
    /// <param name="choices">The selectable rewards.</param>
    public static void Open(Guid characterId, QuestDefinition quest, IList<QuestReward> choices)
    {
        Pending[characterId] = new PendingChoice(quest, choices, DateTime.UtcNow.AddMinutes(2));
    }

    /// <summary>
    /// Tries to get a still-valid pending choice for the character.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="pending">The pending choice when found.</param>
    /// <returns><c>true</c> when a valid pending choice exists.</returns>
    public static bool TryGet(Guid characterId, out PendingChoice pending)
    {
        if (!Pending.TryGetValue(characterId, out pending!))
        {
            return false;
        }

        if (DateTime.UtcNow > pending.ExpiresUtc)
        {
            Pending.TryRemove(characterId, out _);
            pending = null!;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clears any pending choice for the character.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    public static void Clear(Guid characterId) => Pending.TryRemove(characterId, out _);

    /// <summary>
    /// A pending reward selection.
    /// </summary>
    /// <param name="Quest">The quest.</param>
    /// <param name="Choices">The choice rewards in display order.</param>
    /// <param name="ExpiresUtc">When the menu expires.</param>
    internal sealed record PendingChoice(QuestDefinition Quest, IList<QuestReward> Choices, DateTime ExpiresUtc);
}
