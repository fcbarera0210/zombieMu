// <copyright file="AdminOperationCircuitState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using System.Threading;
using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Positively identifies the scoped service provider of a running panel circuit.</summary>
public sealed class AdminOperationCircuitState : CircuitHandler
{
    private readonly ConfigurationEditCoordinator? _editCoordinator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminOperationCircuitState"/> class.
    /// </summary>
    public AdminOperationCircuitState()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminOperationCircuitState"/> class.
    /// </summary>
    /// <param name="editCoordinator">Releases shared edit leases when the circuit closes.</param>
    public AdminOperationCircuitState(ConfigurationEditCoordinator editCoordinator)
    {
        this._editCoordinator = editCoordinator;
    }

    /// <summary>Gets a value indicating whether this scope belongs to an open panel circuit.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Gets the Blazor circuit id while the circuit is open.</summary>
    public string? CircuitId { get; private set; }

    /// <inheritdoc />
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        this.IsOpen = true;
        this.CircuitId = circuit.Id;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        this.IsOpen = false;
        this._editCoordinator?.OnCircuitClosed(circuit.Id);
        this.CircuitId = null;
        return Task.CompletedTask;
    }
}
