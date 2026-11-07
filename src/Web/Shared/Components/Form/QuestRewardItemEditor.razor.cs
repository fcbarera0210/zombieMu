// <copyright file="QuestRewardItemEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.Components.Form;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Focused editor for the item attached to a <see cref="QuestReward"/>.
/// </summary>
public partial class QuestRewardItemEditor
{
    /// <summary>
    /// Gets or sets the reward whose item is edited.
    /// </summary>
    [Parameter]
    public QuestReward Reward { get; set; } = null!;

    /// <summary>
    /// Gets or sets the persistence context of the page that is being edited.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private async Task CreateItemAsync()
    {
        var item = this.PersistenceContext.CreateNew<Item>();
        this.Reward.ItemReward = item;
        this.Reward.RewardType = QuestRewardType.Item;
        if (this.Reward.Value < 1)
        {
            this.Reward.Value = 1;
        }

        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }

    private async Task RemoveItemAsync()
    {
        if (this.Reward.ItemReward is { } item)
        {
            this.Reward.ItemReward = null;
            await this.PersistenceContext.DeleteAsync(item).ConfigureAwait(true);
        }

        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }
}
