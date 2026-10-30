// <copyright file="ConfigurationEditCoordinatorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Services;

using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>Unit tests for <see cref="ConfigurationEditCoordinator"/>.</summary>
[TestFixture]
public sealed class ConfigurationEditCoordinatorTests
{
    /// <summary>A second circuit cannot take the writer lease while the first holds it.</summary>
    [Test]
    public void SecondCircuitCannotAcquireWriterWhileFirstHoldsIt()
    {
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("circuit-a"), Is.True);
        Assert.That(coordinator.TryAcquireWriter("circuit-b"), Is.False);
        Assert.That(coordinator.IsWriter("circuit-a"), Is.True);
        Assert.That(coordinator.CanMutateSharedContext("circuit-b"), Is.False);
        Assert.That(coordinator.CanMutateSharedContext("circuit-a"), Is.True);
    }

    /// <summary>Releasing the writer allows another circuit to mutate the shared context.</summary>
    [Test]
    public void ReleaseWriterAllowsAnotherCircuit()
    {
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("circuit-a"), Is.True);
        coordinator.ReleaseWriter("circuit-a");
        Assert.That(coordinator.TryAcquireWriter("circuit-b"), Is.True);
        Assert.That(coordinator.CanMutateSharedContext("circuit-b"), Is.True);
    }

    /// <summary>Closing a circuit releases its writer lease.</summary>
    [Test]
    public void CircuitClosedReleasesWriter()
    {
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("circuit-a"), Is.True);
        coordinator.OnCircuitClosed("circuit-a");
        Assert.That(coordinator.WriterCircuitId, Is.Null);
        Assert.That(coordinator.TryAcquireWriter("circuit-b"), Is.True);
    }

    /// <summary>SharedContextReset notifies subscribers.</summary>
    [Test]
    public void NotifySharedContextResetRaisesEvent()
    {
        var coordinator = new ConfigurationEditCoordinator();
        var raised = 0;
        coordinator.SharedContextReset += (_, _) => raised++;
        coordinator.NotifySharedContextReset();
        Assert.That(raised, Is.EqualTo(1));
    }

    /// <summary>Releasing a different circuit id does not clear the active writer.</summary>
    [Test]
    public void ReleaseOtherCircuitDoesNotClearWriter()
    {
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("circuit-a"), Is.True);
        coordinator.ReleaseWriter("circuit-b");
        Assert.That(coordinator.IsWriter("circuit-a"), Is.True);
    }
}
