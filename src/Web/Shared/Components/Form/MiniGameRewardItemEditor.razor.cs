// <copyright file="MiniGameRewardItemEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.Components.Form;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Edits the item list that belongs to one mini-game reward.
/// </summary>
public partial class MiniGameRewardItemEditor
{
    private DropItemGroup? _group;

    private bool _sharedWithAnotherReward;

    private static bool SameGroup(DropItemGroup? left, DropItemGroup? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left is IIdentifiable leftId
            && right is IIdentifiable rightId
            && leftId.Id != Guid.Empty
            && leftId.Id == rightId.Id;
    }

    /// <summary>
    /// Gets or sets the reward whose item list is edited.
    /// </summary>
    [Parameter]
    public MiniGameReward Reward { get; set; } = null!;

    /// <summary>
    /// Gets or sets the persistence context of the page that is being edited.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private DropItemGroup Group => this._group ?? throw new InvalidOperationException("The reward has no item list.");

    private ICollection<ItemDefinition> PossibleItems
    {
        get => this.Group.PossibleItems;
        set
        {
            if (value is null || ReferenceEquals(value, this.Group.PossibleItems))
            {
                return;
            }

            this.Group.PossibleItems.Clear();
            foreach (var item in value)
            {
                this.Group.PossibleItems.Add(item);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var group = this.Reward.ItemReward;
        this._group = group;
        this._sharedWithAnotherReward = false;
        if (group is null || !this.GameConfigurationSource.IsSupporting(typeof(GameConfiguration)))
        {
            return;
        }

        var configuration = await this.GameConfigurationSource.GetOwnerAsync().ConfigureAwait(true);
        if (!ReferenceEquals(this.Reward.ItemReward, group))
        {
            return;
        }

        this._sharedWithAnotherReward = configuration.MiniGameDefinitions
            .SelectMany(miniGame => miniGame.Rewards)
            .Count(reward => SameGroup(reward.ItemReward, group)) > 1;
    }

    private async Task CreateGroupAsync()
    {
        var group = this.PersistenceContext.CreateNew<DropItemGroup>();
        group.Chance = 1;
        this.Reward.ItemReward = group;
        await this.AttachToConfigurationAsync(group).ConfigureAwait(true);
        this._group = group;
        this._sharedWithAnotherReward = false;
    }

    private async Task CopyGroupForThisRewardAsync()
    {
        if (this._group is null)
        {
            return;
        }

        var copy = this.PersistenceContext.CreateNew<DropItemGroup>();
        copy.Description = this._group.Description;
        copy.Chance = this._group.Chance;
        copy.ItemType = this._group.ItemType;
        copy.ItemLevel = this._group.ItemLevel;
        copy.MinimumMonsterLevel = this._group.MinimumMonsterLevel;
        copy.MaximumMonsterLevel = this._group.MaximumMonsterLevel;
        if (this._group.PossibleItems is not null)
        {
            foreach (var item in this._group.PossibleItems)
            {
                copy.PossibleItems.Add(item);
            }
        }

        this.Reward.ItemReward = copy;
        await this.AttachToConfigurationAsync(copy).ConfigureAwait(true);
        this._group = copy;
        this._sharedWithAnotherReward = false;
    }

    private async Task AttachToConfigurationAsync(DropItemGroup group)
    {
        if (!this.GameConfigurationSource.IsSupporting(typeof(GameConfiguration)))
        {
            return;
        }

        var configuration = await this.GameConfigurationSource.GetOwnerAsync().ConfigureAwait(true);
        if (!configuration.DropItemGroups.Any(existing => SameGroup(existing, group)))
        {
            configuration.DropItemGroups.Add(group);
        }
    }
}
