// <copyright file="GameConfigurationDropGroupsFieldBuilder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.ComponentBuilders;

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Shared;
using MUnique.OpenMU.Web.Shared.Components.Form;

/// <summary>
/// Shows the game configuration drop catalog without the groups that belong to a mini-game reward.
/// Those groups are edited on the mini game itself.
/// </summary>
public class GameConfigurationDropGroupsFieldBuilder : BaseComponentBuilder, IComponentBuilder
{
    /// <inheritdoc />
    public int BuildComponent(object model, PropertyInfo propertyInfo, RenderTreeBuilder builder, int currentIndex, Services.IChangeNotificationService notificationService)
    {
        if (model is not GameConfiguration configuration)
        {
            return currentIndex;
        }

        var ownedByMiniGame = configuration.MiniGameDefinitions
            .SelectMany(miniGame => miniGame.Rewards)
            .Select(reward => reward.ItemReward)
            .OfType<DropItemGroup>()
            .ToHashSet(DropItemGroupIdentityComparer.Instance);
        var visibleGroups = new DropItemGroupCatalogView(configuration.DropItemGroups, ownedByMiniGame);
#pragma warning disable ASP0006 // Sequence numbers are unique within this builder.
        builder.OpenComponent(currentIndex++, typeof(ItemTable<DropItemGroup>));
        builder.AddAttribute(currentIndex++, nameof(ItemTable<DropItemGroup>.Value), visibleGroups);
        builder.AddAttribute(currentIndex++, nameof(ItemTable<DropItemGroup>.ValueExpression), configuration.CreatePropertyExpression<ICollection<DropItemGroup>>(propertyInfo));
        builder.AddAttribute(
            currentIndex++,
            nameof(ItemTable<DropItemGroup>.ValueChanged),
            EventCallback.Factory.Create<ICollection<DropItemGroup>>(this, _ => { }));
        builder.CloseComponent();
#pragma warning restore ASP0006
        return currentIndex;
    }

    /// <inheritdoc />
    public bool CanBuildComponent(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name == nameof(GameConfiguration.DropItemGroups)
            && propertyInfo.DeclaringType is not null
            && typeof(GameConfiguration).IsAssignableFrom(propertyInfo.DeclaringType);
    }

    /// <summary>
    /// Compares drop groups by reference, and by id when both have been saved.
    /// </summary>
    private sealed class DropItemGroupIdentityComparer : IEqualityComparer<DropItemGroup>
    {
        /// <summary>
        /// Gets the shared comparer.
        /// </summary>
        public static readonly DropItemGroupIdentityComparer Instance = new();

        /// <inheritdoc />
        public bool Equals(DropItemGroup? x, DropItemGroup? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x is IIdentifiable left
                && y is IIdentifiable right
                && left.Id != Guid.Empty
                && left.Id == right.Id;
        }

        /// <inheritdoc />
        public int GetHashCode(DropItemGroup obj)
        {
            return obj is IIdentifiable identifiable && identifiable.Id != Guid.Empty
                ? identifiable.Id.GetHashCode()
                : RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// A view over the shared catalog that hides groups owned by a mini-game reward.
    /// Adds and removes still go to the underlying collection.
    /// </summary>
    private sealed class DropItemGroupCatalogView : ICollection<DropItemGroup>
    {
        private readonly ICollection<DropItemGroup> _source;
        private readonly HashSet<DropItemGroup> _hidden;

        public DropItemGroupCatalogView(ICollection<DropItemGroup> source, HashSet<DropItemGroup> hidden)
        {
            this._source = source;
            this._hidden = hidden;
        }

        public int Count => this._source.Count(group => !this._hidden.Contains(group));

        public bool IsReadOnly => this._source.IsReadOnly;

        public void Add(DropItemGroup item)
        {
            this._source.Add(item);
        }

        public void Clear()
        {
            foreach (var group in this._source.Where(group => !this._hidden.Contains(group)).ToList())
            {
                this._source.Remove(group);
            }
        }

        public bool Contains(DropItemGroup item)
        {
            return !this._hidden.Contains(item) && this._source.Contains(item);
        }

        public void CopyTo(DropItemGroup[] array, int arrayIndex)
        {
            foreach (var group in this)
            {
                array[arrayIndex++] = group;
            }
        }

        public bool Remove(DropItemGroup item)
        {
            return !this._hidden.Contains(item) && this._source.Remove(item);
        }

        public IEnumerator<DropItemGroup> GetEnumerator()
        {
            return this._source.Where(group => !this._hidden.Contains(group)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
