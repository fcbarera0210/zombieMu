// <copyright file="ComplexPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Abstract consume handler for complex potions which recover the <see cref="Stats.CurrentAbility"/>.
/// </summary>
public abstract class ComplexPotionConsumeHandlerPlugIn : RecoverConsumeHandlerPlugIn, IItemConsumeHandlerPlugIn
{
    /// <inheritdoc/>
    protected override AttributeDefinition MaximumAttribute => Stats.MaximumAbility;

    /// <inheritdoc/>
    protected override AttributeDefinition CurrentAttribute => Stats.CurrentAbility;

    /// <summary>
    /// Gets the recover percentage.
    /// </summary>
    protected abstract double RecoverPercentage { get; }

    /// <inheritdoc />
    public sealed override object CreateDefaultConfig()
    {
        return new RecoverConsumeHandlerConfiguration
        {
            TotalRecoverPercentage = this.RecoverPercentage,
            RecoverDelayReductionByPotionLevel = 1.0 / 16.0,
            RecoverPercentageIncreaseByPotionLevel = 1,
            CooldownTime = TimeSpan.FromSeconds(0.5),
            RecoverSteps =
            {
                new RecoverStep
                {
                    Delay = TimeSpan.FromMilliseconds(200),
                    RecoverPercentage = 20,
                },
                new RecoverStep
                {
                    Delay = TimeSpan.FromMilliseconds(600),
                    RecoverPercentage = 60,
                },
                new RecoverStep
                {
                    Delay = TimeSpan.FromMilliseconds(200),
                    RecoverPercentage = 20,
                },
            },
        };
    }
}
