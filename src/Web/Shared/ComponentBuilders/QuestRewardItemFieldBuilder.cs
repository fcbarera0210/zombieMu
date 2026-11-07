// <copyright file="QuestRewardItemFieldBuilder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.ComponentBuilders;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.Web.Shared.Components.Form;

/// <summary>
/// Builds a focused item editor for <see cref="QuestReward.ItemReward"/> instead of the generic aggregate form
/// (which also shows item-set groups and store fields that do not apply to quest rewards).
/// </summary>
public class QuestRewardItemFieldBuilder : IComponentBuilder
{
    /// <inheritdoc />
    public int BuildComponent(object model, PropertyInfo propertyInfo, RenderTreeBuilder builder, int currentIndex, Services.IChangeNotificationService notificationService)
    {
        if (model is not QuestReward reward)
        {
            return currentIndex;
        }

#pragma warning disable ASP0006 // Sequence numbers are unique within this builder.
        builder.OpenComponent<QuestRewardItemEditor>(currentIndex++);
        builder.AddAttribute(currentIndex++, nameof(QuestRewardItemEditor.Reward), reward);
        builder.CloseComponent();
#pragma warning restore ASP0006
        return currentIndex;
    }

    /// <inheritdoc />
    public bool CanBuildComponent(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name == nameof(QuestReward.ItemReward)
            && propertyInfo.DeclaringType is not null
            && typeof(QuestReward).IsAssignableFrom(propertyInfo.DeclaringType);
    }
}
