// <copyright file="UnlockCharacterClassesByLevelRequirementPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Legacy plugin kept for compatibility. Character class creation unlock is evaluated
/// dynamically from <see cref="CharacterClass.LevelRequirementByCreation"/> in
/// <see cref="CharacterCreationUnlockEvaluator"/> (character list and create character).
/// </summary>
/// <remarks>
/// Disabled by default. Per-account <see cref="Account.UnlockedCharacterClasses"/> is not
/// used for creation; configure levels on the character class in the admin panel.
/// </remarks>
[PlugIn]
[Display(
    Name = nameof(PlugInResources.UnlockCharacterClassesByLevelRequirementPlugIn_Name),
    Description = nameof(PlugInResources.UnlockCharacterClassesByLevelRequirementPlugIn_Description),
    ResourceType = typeof(PlugInResources))]
[Guid("7F3A9C2E-4B81-4D6A-9E15-C8F0A2D47B63")]
public class UnlockCharacterClassesByLevelRequirementPlugIn : ICharacterLevelUpPlugIn, IDisabledByDefault
{
    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        // Intentionally empty: unlock flags and create-character checks use
        // CharacterCreationUnlockEvaluator based on account character levels.
    }
}
