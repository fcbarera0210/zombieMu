// <copyright file="ItemOptionEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.ItemEdit;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Edits an <see cref="ItemOptionDefinition"/> or a single <see cref="IncreasableItemOption"/>.
/// Level-dependent power is edited one level at a time instead of nesting a form per row.
/// </summary>
public partial class ItemOptionEditor
{
    private IncreasableItemOption? _selectedOption;

    /// <summary>
    /// Gets or sets the option definition. When set, its possible options are listed beside the editor.
    /// </summary>
    [Parameter]
    public ItemOptionDefinition? Definition { get; set; }

    /// <summary>
    /// Gets or sets a single option, used when the page was opened for that option directly.
    /// </summary>
    [Parameter]
    public IncreasableItemOption? Option { get; set; }

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
    /// Gets or sets the persistence context used to create and delete options and levels.
    /// </summary>
    [CascadingParameter]
    public IContext PersistenceContext { get; set; } = null!;

    private Task AddOptionAsync()
    {
        if (this.Definition is null)
        {
            return Task.CompletedTask;
        }

        var option = this.PersistenceContext.CreateNew<IncreasableItemOption>();
        option.PowerUpDefinition = this.PersistenceContext.CreateNew<PowerUpDefinition>();
        this.Definition.PossibleOptions.Add(option);
        this._selectedOption = option;
        return Task.CompletedTask;
    }

    private async Task RemoveOptionAsync(IncreasableItemOption option)
    {
        if (this.Definition is null)
        {
            return;
        }

        this.Definition.PossibleOptions.Remove(option);
        if (this._selectedOption == option)
        {
            this._selectedOption = null;
        }

        await this.PersistenceContext.DeleteAsync(option).ConfigureAwait(false);
        await this.InvokeAsync(this.StateHasChanged).ConfigureAwait(false);
    }
}
