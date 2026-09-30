// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// The exception that is thrown when a discoverable validator would run in a protected decision command although Arc
/// cannot certify that its rules depend only on the model it validates.
/// </summary>
public class DiscoverableValidatorRefusedInProtectedDecision : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableValidatorRefusedInProtectedDecision"/> class for a
    /// validator that does not have a single parameterless public constructor.
    /// </summary>
    /// <param name="validatorType">The discoverable validator type that was refused.</param>
    public DiscoverableValidatorRefusedInProtectedDecision(Type validatorType)
        : base($"Discoverable validator '{validatorType}' cannot run in a protected decision command because its constructor takes dependencies, or it has more than one public constructor, rather than a single parameterless one. A protected command only runs validators that Arc constructs itself and that validate the command's own input. Move a rule that needs state into Provide or Handle as DecisionRead<T>, or give the validator a single parameterless constructor. See Arc#2831.")
    {
    }

    DiscoverableValidatorRefusedInProtectedDecision(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates the exception for a validator instance the service provider supplied but Arc did not construct.
    /// </summary>
    /// <param name="validatorType">The discoverable validator type that was refused.</param>
    /// <returns>The exception.</returns>
    internal static DiscoverableValidatorRefusedInProtectedDecision NotConstructedByArc(Type validatorType) =>
        new($"Discoverable validator '{validatorType}' cannot run in a protected decision command because the service provider supplied an instance Arc did not construct, such as one from a factory, an instance, an explicit registration or a decorator, or because its rules changed after construction. Do not register the validator yourself or decorate it, and do not add rules to the instance: let convention discovery register it so Arc constructs it. See Arc#2831.");
}
