// <copyright file="CharacterLeaderboards.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Web.AdminPanel.Operations;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.Shared.Components.Toast;

/// <summary>
/// The page which shows the leaderboard of the characters of the server.
/// </summary>
public sealed partial class CharacterLeaderboards : ComponentBase
{
    /// <summary>
    /// Gets the entries of the shown leaderboard.
    /// </summary>
    internal IReadOnlyList<CharacterLeaderboardEntry> Entries { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether the leaderboard is loading.
    /// </summary>
    internal bool IsLoading { get; private set; } = true;

    [Inject]
    private CharacterReadOperations ReadOperations { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<CharacterLeaderboards> Logger { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync().ConfigureAwait(true);
        await this.LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        this.IsLoading = true;
        this.Entries = [];
        try
        {
            var leaderboard = await this.ReadOperations.LeaderboardAsync(CancellationToken.None).ConfigureAwait(true);
            this.Entries = leaderboard.Entries;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error loading the character leaderboard.");
            this.ToastService.ShowError(string.Format(Resources.UnexpectedErrorOccurred, ex.Message));
        }
        finally
        {
            this.IsLoading = false;
        }
    }
}
