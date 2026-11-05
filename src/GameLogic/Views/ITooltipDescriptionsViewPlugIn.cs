// <copyright file="ITooltipDescriptionsViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Shows item and skill tooltip descriptions to an extended client.
/// </summary>
public interface ITooltipDescriptionsViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the tooltip descriptions of the given items and skills.
    /// </summary>
    /// <param name="items">The item definitions which have a description.</param>
    /// <param name="skills">The skills which have a description.</param>
    ValueTask ShowTooltipDescriptionsAsync(IReadOnlyList<ItemDefinition> items, IReadOnlyList<Skill> skills);
}
