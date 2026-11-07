// <copyright file="QuestRewardsEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.Components.Form;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Shared.Components.Form.Modal;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Properties;

/// <summary>
/// Edits quest rewards with a dedicated list for selectable item choices.
/// </summary>
public partial class QuestRewardsEditor
{
    /// <summary>
    /// Gets or sets the label which should be displayed.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the persistence context.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private IList<QuestReward> ChoiceRewards =>
        (this.CurrentValue ?? Enumerable.Empty<QuestReward>())
            .Where(IsChoiceItemReward)
            .ToList();

    private IList<QuestReward> FixedRewards =>
        (this.CurrentValue ?? Enumerable.Empty<QuestReward>())
            .Where(reward => !IsChoiceItemReward(reward))
            .ToList();

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out ICollection<QuestReward> result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        throw new NotImplementedException();
    }

    private static bool IsChoiceItemReward(QuestReward reward)
        => reward.IsChoiceOption && reward.RewardType == QuestRewardType.Item;

    private async Task AddChoiceItemsAsync()
    {
        var parameters = new ModalParameters();
        parameters.Add(nameof(ModalObjectMultiSelection<ItemDefinition>.PersistenceContext), this.PersistenceContext);

        var modal = this.ModalService.Show<ModalObjectMultiSelection<ItemDefinition>>(
            string.Format(Resources.SelectType, typeof(ItemDefinition).GetTypeCaption()),
            parameters);
        var result = await modal.Result.ConfigureAwait(true);
        if (result.Cancelled || result.Data is not IList<ItemDefinition> definitions || definitions.Count == 0)
        {
            return;
        }

        this.CurrentValue ??= new List<QuestReward>();
        foreach (var definition in definitions)
        {
            var reward = this.PersistenceContext.CreateNew<QuestReward>();
            reward.RewardType = QuestRewardType.Item;
            reward.IsChoiceOption = true;
            reward.Value = 1;
            reward.ItemReward = this.CreateItemFromDefinition(definition);
            this.CurrentValue.Add(reward);
        }

        this.EditContext.NotifyFieldChanged(this.FieldIdentifier);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }

    private async Task CreateOtherRewardAsync()
    {
        var reward = this.PersistenceContext.CreateNew<QuestReward>();
        var editorResult = await this.ShowRewardEditorAsync(reward, isNew: true).ConfigureAwait(true);
        if (editorResult.Cancelled)
        {
            await this.PersistenceContext.DeleteAsync(reward).ConfigureAwait(true);
            return;
        }

        this.CurrentValue ??= new List<QuestReward>();
        this.CurrentValue.Add(reward);
        this.EditContext.NotifyFieldChanged(this.FieldIdentifier);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }

    private async Task EditRewardAsync(QuestReward reward)
    {
        await this.ShowRewardEditorAsync(reward, isNew: false).ConfigureAwait(true);
        this.EditContext.NotifyFieldChanged(this.FieldIdentifier);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }

    private async Task RemoveRewardAsync(QuestReward reward)
    {
        this.CurrentValue?.Remove(reward);
        this.EditContext.NotifyFieldChanged(this.FieldIdentifier);
        if (this.PersistenceContext.IsSupporting(typeof(QuestReward)))
        {
            if (reward.ItemReward is { } item)
            {
                reward.ItemReward = null;
                await this.PersistenceContext.DeleteAsync(item).ConfigureAwait(true);
            }

            await this.PersistenceContext.DeleteAsync(reward).ConfigureAwait(true);
        }

        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(true);
    }

    private Task<ModalResult> ShowRewardEditorAsync(QuestReward reward, bool isNew)
    {
        var parameters = new ModalParameters();
        parameters.Add(nameof(ModalCreateNew<QuestReward>.Item), reward);
        parameters.Add(nameof(ModalCreateNew<QuestReward>.PersistenceContext), this.PersistenceContext);
        parameters.Add(nameof(ModalCreateNew<QuestReward>.IsNew), isNew);
        parameters.Add(nameof(ModalCreateNew<QuestReward>.ShowDeferredSaveHint), true);

        var options = new ModalOptions
        {
            DisableBackgroundCancel = true,
            Size = ModalSize.ExtraLarge,
        };

        var caption = typeof(QuestReward).GetTypeCaption();
        var title = isNew
            ? string.Format(Resources.CreateType, caption)
            : string.Format(Resources.EditType, caption);
        var modal = this.ModalService.Show<ModalCreateNew<QuestReward>>(title, parameters, options);
        return modal.Result;
    }

    private Item CreateItemFromDefinition(ItemDefinition definition)
    {
        var item = this.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = item.IsWearable()
            ? item.GetMaximumDurabilityOfOnePiece()
            : Math.Max(1, (int)definition.Durability);
        return item;
    }
}
