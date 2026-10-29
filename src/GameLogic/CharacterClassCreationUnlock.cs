// <copyright file="CharacterClassCreationUnlock.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Helpers for unlocking character class creation based only on
/// <see cref="CharacterClass.LevelRequirementByCreation"/>.
/// </summary>
/// <remarks>
/// <see cref="Account.UnlockedCharacterClasses"/> is intentionally ignored for creation
/// and character-list flags, so new accounts work without any per-account unlock setup.
/// </remarks>
public static class CharacterClassCreationUnlock
{
    /// <summary>
    /// Gets the highest character level of the account (0 when it has no characters).
    /// </summary>
    /// <param name="account">The account.</param>
    /// <returns>The highest level.</returns>
    public static int GetHighestCharacterLevel(Account account)
    {
        if (account.Characters is not { Count: > 0 })
        {
            return 0;
        }

        return account.Characters
            .Select(GetCharacterLevel)
            .DefaultIfEmpty(0)
            .Max();
    }

    /// <summary>
    /// Determines whether the account may create a character of the given class.
    /// Base classes (no creation unlock flag) are always allowed.
    /// Special classes are allowed when the highest character level meets
    /// <see cref="CharacterClass.LevelRequirementByCreation"/>.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="characterClass">The character class.</param>
    /// <returns><see langword="true"/> when creation is allowed.</returns>
    public static bool IsCreationUnlocked(Account account, CharacterClass characterClass)
    {
        if (characterClass.CreationAllowedFlag == 0)
        {
            return true;
        }

        return GetHighestCharacterLevel(account) >= characterClass.LevelRequirementByCreation;
    }

    /// <summary>
    /// Builds the unlock flags which are sent to the client on the character list.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="configuration">The game configuration.</param>
    /// <returns>The aggregated creation unlock flags.</returns>
    public static byte GetUnlockFlags(Account account, GameConfiguration configuration)
    {
        byte flags = 0;
        foreach (var characterClass in GetUnlockableClasses(configuration))
        {
            if (IsCreationUnlocked(account, characterClass))
            {
                flags |= characterClass.CreationAllowedFlag;
            }
        }

        return flags;
    }

    private static IEnumerable<CharacterClass> GetUnlockableClasses(GameConfiguration configuration)
    {
        return configuration.CharacterClasses
            .Where(c => c is { IsActive: true, CanGetCreated: true, CreationAllowedFlag: > 0 });
    }

    private static int GetCharacterLevel(Character character)
    {
        return (int)(character.Attributes.FirstOrDefault(a => a.Definition == Stats.Level)?.Value ?? 1);
    }
}
