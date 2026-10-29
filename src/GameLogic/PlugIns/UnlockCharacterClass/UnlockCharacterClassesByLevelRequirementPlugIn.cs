// <copyright file="UnlockCharacterClassesByLevelRequirementPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Unlocks character classes for an account based on
/// <see cref="CharacterClass.LevelRequirementByCreation"/> from the game configuration.
/// </summary>
/// <remarks>
/// Replaces the hard-coded level unlock plugins. Change the required levels in the admin
/// panel on each <see cref="CharacterClass"/> (Level Requirement By Creation); the next
/// level-up that meets the requirement unlocks the class for that account.
/// Classes without a creation unlock flag (<see cref="CharacterClass.CreationAllowedFlag"/>)
/// are ignored. A requirement of 0 means the class unlocks on the first level-up.
/// </remarks>
[PlugIn]
[Display(
    Name = nameof(PlugInResources.UnlockCharacterClassesByLevelRequirementPlugIn_Name),
    Description = nameof(PlugInResources.UnlockCharacterClassesByLevelRequirementPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[Guid("7F3A9C2E-4B81-4D6A-9E15-C8F0A2D47B63")]
public class UnlockCharacterClassesByLevelRequirementPlugIn : ICharacterLevelUpPlugIn
{
    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        // Server-side bots are excluded: they never create new characters, and animating several
        // characters of one account concurrently (each with its own persistence context and thus its
        // own stale copy of the account) lets two siblings pass the duplicate check below at the same
        // time - both insert the same unlock row and the account's saves keep failing with a duplicate
        // key of "PK_AccountCharacterClass" until the next restart.
        if (player.Account is not { IsBot: false } account)
        {
            return;
        }

        var unlockableClasses = player.GameContext.Configuration.CharacterClasses
            .Where(c => c is { IsActive: true, CanGetCreated: true, CreationAllowedFlag: > 0 })
            .Where(c => player.Level >= c.LevelRequirementByCreation)
            .Where(c => account.UnlockedCharacterClasses.All(unlocked => unlocked.Number != c.Number))
            .ToList();

        foreach (var characterClass in unlockableClasses)
        {
            account.UnlockedCharacterClasses.Add(characterClass);
        }
    }
}
