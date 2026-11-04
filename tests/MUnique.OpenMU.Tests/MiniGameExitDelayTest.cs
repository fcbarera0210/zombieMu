// <copyright file="MiniGameExitDelayTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;

/// <summary>
/// Tests that finishing a mini game early keeps players on the map for the configured
/// exit/loot window instead of warping them out on the same tick as the clear.
/// </summary>
[TestFixture]
public class MiniGameExitDelayTest
{
    private static readonly TimeSpan ExitWindow = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly List<IAsyncDisposable> _disposables = [];

    /// <summary>
    /// Disposes created games.
    /// </summary>
    [TearDown]
    public async ValueTask TearDownAsync()
    {
        foreach (var disposable in this._disposables)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

        this._disposables.Clear();
    }

    /// <summary>
    /// After an early finish, the player must stay registered and on the event map
    /// until the exit duration elapses, and only then be cleared for warp-out.
    /// </summary>
    [Test]
    public async Task EarlyFinishKeepsPlayersUntilExitDurationElapsesAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var game = this.CreateGame(player.GameContext);
        Assert.That(await game.TryEnterAsync(player).ConfigureAwait(false), Is.EqualTo(EnterResult.Success));
        await game.Map.AddAsync(player).ConfigureAwait(false);

        await WaitUntilAsync(() => game.State == MiniGameState.Playing).ConfigureAwait(false);
        game.Finish();

        await WaitUntilAsync(() => game.State == MiniGameState.Ended).ConfigureAwait(false);
        await Task.Delay(ExitWindow / 2).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(game.State, Is.EqualTo(MiniGameState.Ended));
            Assert.That(game.IsDisposed, Is.False);
            Assert.That(player.CurrentMiniGame, Is.SameAs(game));
            Assert.That(player.CurrentMap, Is.SameAs(game.Map));
            Assert.That(game.RegisteredPlayerCount, Is.EqualTo(1));
        });

        await WaitUntilAsync(() => game.IsDisposed || player.CurrentMiniGame is null).ConfigureAwait(false);

        Assert.That(player.CurrentMiniGame, Is.Null);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.Add(Timeout);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20).ConfigureAwait(false);
        }

        Assert.That(condition(), Is.True, "The condition wasn't met in time.");
    }

    private TestGame CreateGame(IGameContext gameContext)
    {
        var safezoneMap = new GameMapDefinition { Number = 2 };
        safezoneMap.ExitGates.Add(new ExitGate
        {
            Map = safezoneMap,
            IsSpawnGate = true,
            X1 = 10,
            X2 = 10,
            Y1 = 10,
            Y2 = 10,
        });
        var mapDefinition = new GameMapDefinition
        {
            Number = 11,
            SafezoneMap = safezoneMap,
        };
        mapDefinition.ExitGates.Add(new ExitGate
        {
            Map = safezoneMap,
            IsSpawnGate = true,
            X1 = 10,
            X2 = 10,
            Y1 = 10,
            Y2 = 10,
        });

        var definitionMock = new Mock<MiniGameDefinition>();
        definitionMock.SetupGet(d => d.Rewards).Returns(new List<MiniGameReward>());
        definitionMock.SetupGet(d => d.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definitionMock.SetupGet(d => d.ChangeEvents).Returns(new List<MiniGameChangeEvent>());
        definitionMock.SetupGet(d => d.Entrance).Returns(new ExitGate { Map = mapDefinition, X1 = 20, X2 = 20, Y1 = 20, Y2 = 20 });
        var definition = definitionMock.Object;
        definition.Type = MiniGameType.BloodCastle;
        definition.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
        definition.EnterDuration = TimeSpan.Zero;
        definition.GameDuration = TimeSpan.FromMinutes(5);
        definition.ExitDuration = ExitWindow;
        definition.MaximumPlayerCount = 10;

        var mapInitializerMock = new Mock<IMapInitializer>();
        mapInitializerMock.Setup(m => m.CreateGameMap(It.IsAny<GameMapDefinition>()))
            .Returns<GameMapDefinition>(map => new GameMap(map, TimeSpan.FromMinutes(1), 16));
        mapInitializerMock.Setup(m => m.InitializeNpcsOnEventStartAsync(It.IsAny<GameMap>(), It.IsAny<IEventStateProvider>()))
            .Returns(ValueTask.CompletedTask);

        var game = new TestGame(new MiniGameMapKey(mapDefinition.Number, definition.GameLevel, string.Empty), definition, gameContext, mapInitializerMock.Object);
        this._disposables.Add(game);
        game.EnsureGameLoopRunning();
        return game;
    }

    /// <summary>
    /// A game which starts without waiting and uses a short exit countdown for tests.
    /// </summary>
    private sealed class TestGame : MiniGameContext
    {
        public TestGame(MiniGameMapKey key, MiniGameDefinition definition, IGameContext gameContext, IMapInitializer mapInitializer)
            : base(key, definition, gameContext, mapInitializer)
        {
        }

        protected override TimeSpan MinimumEnterDuration => TimeSpan.Zero;

        protected override TimeSpan CountdownDuration => TimeSpan.Zero;

        protected override TimeSpan ExitCountdownDuration => ExitWindow;

        public void Finish() => this.FinishEvent();
    }
}
