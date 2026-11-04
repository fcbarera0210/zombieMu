// <copyright file="ItemDefinitionEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.ItemEdit;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Shared.Properties;

/// <summary>
/// Edits an <see cref="ItemDefinition"/> in sections, with the inventory sprite beside the fields.
/// Requirements and base power are edited in place. Larger related objects stay on their own pages.
/// </summary>
public partial class ItemDefinitionEditor
{
    private ItemEditorTab _tab = ItemEditorTab.General;
    private byte _previewLevel;
    private bool _previewExcellent;
    private bool _previewAncient;

    private enum ItemEditorTab
    {
        General,
        Usage,
        Drop,
        Requirements,
        Power,
        Relations,
        Boxes,
    }

    /// <summary>
    /// Gets or sets the item definition to edit.
    /// </summary>
    [Parameter]
    public ItemDefinition Item { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback invoked when the form is saved.
    /// </summary>
    [Parameter]
    public EventCallback OnValidSubmit { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user asks to reload the object.
    /// </summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a save is already running.
    /// </summary>
    [Parameter]
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Gets or sets the persistence context used to create and delete child rows.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private ICollection<CharacterClass> QualifiedCharacters
    {
        get => this.Item.QualifiedCharacters;
        set => _ = value;
    }

    private ICollection<ItemSetGroup> PossibleItemSetGroups
    {
        get => this.Item.PossibleItemSetGroups;
        set => _ = value;
    }

    private ICollection<ItemOptionDefinition> PossibleItemOptions
    {
        get => this.Item.PossibleItemOptions;
        set => _ = value;
    }

    private ICollection<ItemDropItemGroup> DropItems
    {
        get => this.Item.DropItems;
        set => _ = value;
    }

    private string TabCaption(ItemEditorTab tab)
    {
        return tab switch
        {
            ItemEditorTab.General => Resources.ItemEditorTabGeneral,
            ItemEditorTab.Usage => Resources.ItemEditorTabUsage,
            ItemEditorTab.Drop => Resources.ItemEditorTabDrop,
            ItemEditorTab.Requirements => Resources.ItemEditorTabRequirements,
            ItemEditorTab.Power => Resources.ItemEditorTabPower,
            ItemEditorTab.Relations => Resources.ItemEditorTabRelations,
            ItemEditorTab.Boxes => Resources.ItemEditorTabBoxes,
            _ => tab.ToString(),
        };
    }

    private Task AddRequirementAsync()
    {
        this.Item.Requirements.Add(this.PersistenceContext.CreateNew<AttributeRequirement>());
        return Task.CompletedTask;
    }

    private async Task RemoveRequirementAsync(AttributeRequirement requirement)
    {
        this.Item.Requirements.Remove(requirement);
        await this.PersistenceContext.DeleteAsync(requirement).ConfigureAwait(false);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(false);
    }

    private Task AddPowerUpAsync()
    {
        this.Item.BasePowerUpAttributes.Add(this.PersistenceContext.CreateNew<ItemBasePowerUpDefinition>());
        return Task.CompletedTask;
    }

    private async Task RemovePowerUpAsync(ItemBasePowerUpDefinition powerUp)
    {
        this.Item.BasePowerUpAttributes.Remove(powerUp);
        await this.PersistenceContext.DeleteAsync(powerUp).ConfigureAwait(false);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(false);
    }
}
