// <copyright file="EditorMultiTabIsolationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Pages;

using System.Threading;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Pages;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.AdminPanel.Services;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Shared.Services;
using MUnique.OpenMU.Web.Tests.NetworkAnalyzer;

/// <summary>Ensures one Blazor tab can edit while another only reads the shared configuration.</summary>
[TestFixture]
public sealed class EditorMultiTabIsolationTests
{
    /// <summary>Disposing a reader editor must not discard the writer tab's pending shared context.</summary>
    [Test]
    public async Task DisposingReaderEditorPreservesWriterContextAsync()
    {
        using var context = new BunitContext();
        var configuration = new GameConfiguration { ExperienceRate = 3 };
        var persistenceContext = CreateContext(configuration);
        persistenceContext.SetupGet(c => c.HasChanges).Returns(true);
        var provider = new Mock<IPersistenceContextProvider>();
        provider.Setup(p => p.CreateNewContext()).Returns(persistenceContext.Object);
        using IDataSource<GameConfiguration> source = new GameConfigurationDataSource(NullLogger<GameConfigurationDataSource>.Instance, provider.Object);
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("merchant-tab"), Is.True);

        Configure(context, source, provider.Object, coordinator, "items-tab");
        var editor = context.Render<EditConfig>(p => p.Add(e => e.TypeString, typeof(GameConfiguration).FullName!));
        editor.WaitForState(() => editor.FindAll("form").Count == 1);
        Assert.That(coordinator.IsWriter("merchant-tab"), Is.True);
        Assert.That(coordinator.IsWriter("items-tab"), Is.False);

        await editor.Instance.DisposeAsync().ConfigureAwait(false);

        persistenceContext.Verify(c => c.Dispose(), Times.Never);
        Assert.That(await source.GetOwnerAsync().ConfigureAwait(false), Is.SameAs(configuration));
        Assert.That(coordinator.IsWriter("merchant-tab"), Is.True);
    }

    /// <summary>A second shared editor cannot acquire the writer while another tab holds it.</summary>
    [Test]
    public void SecondEditorShowsBlockedToastWhenWriterIsHeld()
    {
        using var context = new BunitContext();
        var configuration = new GameConfiguration { ExperienceRate = 3 };
        var persistenceContext = CreateContext(configuration);
        var provider = new Mock<IPersistenceContextProvider>();
        provider.Setup(p => p.CreateNewContext()).Returns(persistenceContext.Object);
        using IDataSource<GameConfiguration> source = new GameConfigurationDataSource(NullLogger<GameConfigurationDataSource>.Instance, provider.Object);
        var coordinator = new ConfigurationEditCoordinator();
        Assert.That(coordinator.TryAcquireWriter("merchant-tab"), Is.True);

        var toast = Configure(context, source, provider.Object, coordinator, "items-tab");
        var editor = context.Render<EditConfig>(p => p.Add(e => e.TypeString, typeof(GameConfiguration).FullName!));
        editor.WaitForState(() => editor.FindAll("form").Count == 1);
        toast.Verify(t => t.ShowError(Resources.SaveOrDiscardChangesFirst, null), Times.Once);
        Assert.That(coordinator.IsWriter("merchant-tab"), Is.True);
    }

    private static Mock<IContext> CreateContext(GameConfiguration configuration)
    {
        var persistenceContext = new Mock<IContext>();
        persistenceContext.Setup(c => c.GetAsync<GameConfiguration>(It.IsAny<CancellationToken>())).ReturnsAsync([configuration]);
        persistenceContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return persistenceContext;
    }

    private static Mock<IToastService> Configure(
        BunitContext context,
        IDataSource<GameConfiguration> source,
        IPersistenceContextProvider provider,
        ConfigurationEditCoordinator coordinator,
        string participantId)
    {
        var toast = new Mock<IToastService>();
        var circuitState = new AdminOperationCircuitState(coordinator);
        context.Services.AddSingleton(source);
        context.Services.AddSingleton(provider);
        context.Services.AddSingleton(toast.Object);
        context.Services.AddSingleton<IModalService>(new TestModalService(true));
        context.Services.AddSingleton<LoadingOverlayService>();
        context.Services.AddSingleton<NavigationHistory>();
        context.Services.AddSingleton<IChangeNotificationService, ChangeNotificationService>();
        context.Services.AddSingleton(coordinator);
        context.Services.AddSingleton(circuitState);
        context.Services.AddSingleton(new ConfigurationEditParticipant(circuitState, participantId));
        context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/edit-config/" + typeof(GameConfiguration).FullName + "?search=ExperienceRate");
        return toast;
    }
}
