// <copyright file="UnlockSummonerAtLevel1.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Unlocks the Summoner character class as soon as the first character of an account reaches level 1.
/// </summary>
/// <remarks>
/// Disabled by default in favor of <see cref="UnlockCharacterClassesByLevelRequirementPlugIn"/>,
/// which reads <see cref="CharacterClass.LevelRequirementByCreation"/> from the admin configuration.
/// Summoner starts with requirement 0, so it unlocks on the first level-up unless you raise it.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.UnlockSummonerAtLevel1_Name), Description = nameof(PlugInResources.UnlockSummonerAtLevel1_Description), ResourceType = typeof(PlugInResources))]
[Guid("2DFFD751-7651-4FA1-93D1-9890CA0F04F1")]
public class UnlockSummonerAtLevel1 : UnlockCharacterAtLevelBase, IDisabledByDefault
{
    private const byte SummonerNumber = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnlockSummonerAtLevel1"/> class.
    /// </summary>
    public UnlockSummonerAtLevel1()
        : base(SummonerNumber, 1, "Summoner")
    {
    }
}