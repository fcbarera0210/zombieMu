// <copyright file="IncreasableItemOptionPanel.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.ItemEdit;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Edits one <see cref="IncreasableItemOption"/> and opens a single power-up form for the selected level.
/// </summary>
public partial class IncreasableItemOptionPanel
{
    private ItemOptionOfLevel? _selectedLevel;

    /// <summary>
    /// Gets or sets the option to edit.
    /// </summary>
    [Parameter]
    public IncreasableItemOption Option { get; set; } = null!;

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
    /// Gets or sets a value indicating whether this panel renders its own save button.
    /// The surrounding option-definition editor already has one.
    /// </summary>
    [Parameter]
    public bool ShowSave { get; set; }

    /// <summary>
    /// Gets or sets the persistence context used to create and delete level rows.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private Task AddLevelAsync()
    {
        var levelOption = this.PersistenceContext.CreateNew<ItemOptionOfLevel>();
        levelOption.PowerUpDefinition = this.PersistenceContext.CreateNew<PowerUpDefinition>();
        var nextLevel = this.Option.LevelDependentOptions.Count == 0
            ? 1
            : this.Option.LevelDependentOptions.Max(option => option.Level) + 1;
        levelOption.Level = nextLevel;
        this.Option.LevelDependentOptions.Add(levelOption);
        this._selectedLevel = levelOption;
        return Task.CompletedTask;
    }

    private async Task RemoveLevelAsync(ItemOptionOfLevel levelOption)
    {
        this.Option.LevelDependentOptions.Remove(levelOption);
        if (this._selectedLevel == levelOption)
        {
            this._selectedLevel = null;
        }

        await this.PersistenceContext.DeleteAsync(levelOption).ConfigureAwait(false);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(false);
    }
}
