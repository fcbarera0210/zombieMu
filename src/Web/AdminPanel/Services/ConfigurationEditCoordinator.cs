// <copyright file="ConfigurationEditCoordinator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// Coordinates which Blazor circuit may mutate the shared
/// <see cref="MUnique.OpenMU.Persistence.IDataSource{T}"/> for
/// <see cref="MUnique.OpenMU.DataModel.Configuration.GameConfiguration"/>.
/// </summary>
/// <remarks>
/// The data source is a process-wide singleton. Multiple browser tabs share it.
/// One circuit may hold the writer lease (e.g. Merchants); other circuits may still
/// read. Destructive operations (<c>Discard</c> / <c>ForceDiscard</c>) are limited
/// to the writer so a second tab cannot wipe pending edits.
/// </remarks>
public sealed class ConfigurationEditCoordinator
{
    private readonly object _gate = new();
    private string? _writerCircuitId;

    /// <summary>
    /// Raised after the shared configuration context was reset and readers should reload.
    /// </summary>
    public event EventHandler? SharedContextReset;

    /// <summary>
    /// Gets the circuit id that currently holds the writer lease, if any.
    /// </summary>
    public string? WriterCircuitId
    {
        get
        {
            lock (this._gate)
            {
                return this._writerCircuitId;
            }
        }
    }

    /// <summary>
    /// Tries to acquire the writer lease for <paramref name="circuitId"/>.
    /// </summary>
    /// <param name="circuitId">The circuit (or scoped fallback) identifier.</param>
    /// <returns>
    /// <see langword="true"/> if this circuit holds the lease afterwards;
    /// <see langword="false"/> if another circuit already holds it.
    /// </returns>
    public bool TryAcquireWriter(string circuitId)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        lock (this._gate)
        {
            if (this._writerCircuitId is null || this._writerCircuitId == circuitId)
            {
                this._writerCircuitId = circuitId;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="circuitId"/> currently holds the writer lease.
    /// </summary>
    /// <param name="circuitId">The circuit identifier.</param>
    /// <returns><see langword="true"/> if this circuit is the writer.</returns>
    public bool IsWriter(string circuitId)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        lock (this._gate)
        {
            return this._writerCircuitId == circuitId;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="circuitId"/> may discard or force-reset
    /// the shared configuration context.
    /// </summary>
    /// <param name="circuitId">The circuit identifier.</param>
    /// <returns>
    /// <see langword="true"/> when nobody holds the lease, or when this circuit is the writer.
    /// </returns>
    public bool CanMutateSharedContext(string circuitId)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        lock (this._gate)
        {
            return this._writerCircuitId is null || this._writerCircuitId == circuitId;
        }
    }

    /// <summary>
    /// Releases the writer lease if held by <paramref name="circuitId"/>.
    /// </summary>
    /// <param name="circuitId">The circuit identifier.</param>
    public void ReleaseWriter(string circuitId)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        lock (this._gate)
        {
            if (this._writerCircuitId == circuitId)
            {
                this._writerCircuitId = null;
            }
        }
    }

    /// <summary>
    /// Called when a Blazor circuit closes so a abandoned writer lease cannot stick forever.
    /// </summary>
    /// <param name="circuitId">The closed circuit identifier.</param>
    public void OnCircuitClosed(string circuitId)
    {
        this.ReleaseWriter(circuitId);
    }

    /// <summary>
    /// Notifies subscribers that the shared configuration context was reset and should be reloaded.
    /// </summary>
    public void NotifySharedContextReset()
    {
        this.SharedContextReset?.Invoke(this, EventArgs.Empty);
    }
}
