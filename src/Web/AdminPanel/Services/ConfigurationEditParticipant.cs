// <copyright file="ConfigurationEditParticipant.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Per-circuit identity used with <see cref="ConfigurationEditCoordinator"/>.
/// </summary>
/// <remarks>
/// Prefer <see cref="AdminOperationCircuitState.CircuitId"/> when the Blazor circuit
/// is open. Outside a circuit (tests, early resolve) a stable scoped fallback is used.
/// </remarks>
public sealed class ConfigurationEditParticipant
{
    private readonly AdminOperationCircuitState _circuitState;
    private readonly string _fallbackId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationEditParticipant"/> class.
    /// </summary>
    /// <param name="circuitState">The circuit lifecycle marker for this scope.</param>
    [ActivatorUtilitiesConstructor]
    public ConfigurationEditParticipant(AdminOperationCircuitState circuitState)
        : this(circuitState, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationEditParticipant"/> class.
    /// </summary>
    /// <param name="circuitState">The circuit lifecycle marker for this scope.</param>
    /// <param name="fallbackId">
    /// Optional fixed fallback id (useful in tests). When <paramref name="circuitState"/> has no
    /// <see cref="AdminOperationCircuitState.CircuitId"/>, this value is used instead of a random guid.
    /// </param>
    public ConfigurationEditParticipant(AdminOperationCircuitState circuitState, string? fallbackId)
    {
        this._circuitState = circuitState;
        this._fallbackId = string.IsNullOrEmpty(fallbackId) ? Guid.NewGuid().ToString("N") : fallbackId;
    }

    /// <summary>
    /// Gets the identifier of this edit participant.
    /// </summary>
    public string Id => this._circuitState.CircuitId ?? this._fallbackId;
}
