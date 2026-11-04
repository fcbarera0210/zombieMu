// <copyright file="MiniGameRewardItemFieldBuilder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.ComponentBuilders;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Web.Shared.Components.Form;

/// <summary>
/// Edits the item list of one <see cref="MiniGameReward"/> instead of the shared drop-group catalog.
/// </summary>
public class MiniGameRewardItemFieldBuilder : IComponentBuilder
{
    /// <inheritdoc />
    public int BuildComponent(object model, PropertyInfo propertyInfo, RenderTreeBuilder builder, int currentIndex, Services.IChangeNotificationService notificationService)
    {
        if (model is not MiniGameReward reward)
        {
            return currentIndex;
        }

#pragma warning disable ASP0006 // Sequence numbers are unique within this builder.
        builder.OpenComponent<MiniGameRewardItemEditor>(currentIndex++);
        builder.AddAttribute(currentIndex++, nameof(MiniGameRewardItemEditor.Reward), reward);
        builder.CloseComponent();
#pragma warning restore ASP0006
        return currentIndex;
    }

    /// <inheritdoc />
    public bool CanBuildComponent(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name == nameof(MiniGameReward.ItemReward)
            && propertyInfo.DeclaringType is not null
            && typeof(MiniGameReward).IsAssignableFrom(propertyInfo.DeclaringType);
    }
}
