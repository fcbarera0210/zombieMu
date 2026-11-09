// <copyright file="CharacterReadOperationsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Leaderboards;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.Operations;

/// <summary>
/// Tests for the <see cref="CharacterReadOperations"/>.
/// </summary>
[TestFixture]
public class CharacterReadOperationsTests
{
    private InMemoryPersistenceContextProvider _persistenceContextProvider = null!;
    private CharacterReadOperations _operations = null!;

    /// <summary>
    /// Sets up the in-memory persistence and the operations.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._persistenceContextProvider = new InMemoryPersistenceContextProvider();
        this._operations = new CharacterReadOperations(this._persistenceContextProvider);
    }

    /// <summary>
    /// The leaderboard ranks characters by resets, master level and level, and excludes bots and game masters.
    /// </summary>
    [Test]
    public async Task LeaderboardRanksCharactersAsync()
    {
        using (var context = this._persistenceContextProvider.CreateNewPlayerContext(new GameConfiguration()))
        {
            var characterClass = context.CreateNew<CharacterClass>();
            characterClass.Name = "Dark Knight";
            AddAccount(context, characterClass, isBot: false, ("Normal", 400, 0, 0, CharacterStatus.Normal), ("Master", 400, 50, 0, CharacterStatus.Normal));
            AddAccount(context, characterClass, isBot: false, ("Reset", 100, 0, 1, CharacterStatus.Normal), ("GameMaster", 400, 200, 5, CharacterStatus.GameMaster));
            AddAccount(context, characterClass, isBot: true, ("Bot", 400, 200, 5, CharacterStatus.Normal));
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var leaderboard = await this._operations.LeaderboardAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.That(leaderboard.Entries, Is.EqualTo(new[]
        {
            new CharacterLeaderboardEntry(1, "Reset", "Dark Knight", 100, 0, 1),
            new CharacterLeaderboardEntry(2, "Master", "Dark Knight", 400, 50, 0),
            new CharacterLeaderboardEntry(3, "Normal", "Dark Knight", 400, 0, 0),
        }));
    }

    /// <summary>
    /// An empty server has an empty leaderboard.
    /// </summary>
    [Test]
    public async Task EmptyServerHasEmptyLeaderboardAsync()
    {
        var leaderboard = await this._operations.LeaderboardAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.That(leaderboard.Entries, Is.Empty);
    }

    private static void AddAccount(IContext context, CharacterClass characterClass, bool isBot, params (string Name, int Level, int MasterLevel, int Resets, CharacterStatus Status)[] characters)
    {
        var account = context.CreateNew<Account>();
        account.LoginName = Guid.NewGuid().ToString("N")[..10];
        account.IsBot = isBot;
        foreach (var (name, level, masterLevel, resets, status) in characters)
        {
            var character = context.CreateNew<Character>();
            character.Name = name;
            character.CharacterClass = characterClass;
            character.CharacterStatus = status;
            character.Attributes.Add(new Persistence.BasicModel.StatAttribute(Stats.Level, level));
            character.Attributes.Add(new Persistence.BasicModel.StatAttribute(Stats.MasterLevel, masterLevel));
            character.Attributes.Add(new Persistence.BasicModel.StatAttribute(Stats.Resets, resets));
            account.Characters.Add(character);
        }
    }
}
