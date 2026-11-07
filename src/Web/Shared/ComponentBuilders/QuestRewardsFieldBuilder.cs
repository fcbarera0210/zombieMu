// <copyright file="QuestRewardsFieldBuilder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.ComponentBuilders;

using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Builds the specialized rewards editor for <see cref="QuestDefinition.Rewards"/>,
/// so several selectable item options can be added as separate choice rewards.
/// </summary>
public class QuestRewardsFieldBuilder : BaseComponentBuilder, IComponentBuilder
{
    /// <inheritdoc />
    public int BuildComponent(object model, PropertyInfo propertyInfo, RenderTreeBuilder builder, int currentIndex, IChangeNotificationService notificationService)
        => this.BuildField<ICollection<QuestReward>, QuestRewardsEditor>(model, propertyInfo, builder, currentIndex, notificationService);

    /// <inheritdoc />
    public bool CanBuildComponent(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name == nameof(QuestDefinition.Rewards)
            && propertyInfo.DeclaringType is not null
            && typeof(QuestDefinition).IsAssignableFrom(propertyInfo.DeclaringType);
    }
}
