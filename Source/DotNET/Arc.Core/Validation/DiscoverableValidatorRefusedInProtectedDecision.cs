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
    /// validator whose constructor takes dependencies.
    /// </summary>
    /// <param name="validatorType">The discoverable validator type that was refused.</param>
    public DiscoverableValidatorRefusedInProtectedDecision(Type validatorType)
        : base($"Discoverable validator '{validatorType}' cannot run in a protected decision command because its constructor takes dependencies. A protected command only runs validators with a single parameterless constructor that validate the command's own input; move reads into Provide or Handle as DecisionRead<T>. See Arc#2831.")
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
        new($"Discoverable validator '{validatorType}' cannot run in a protected decision command because the service provider supplied an instance Arc did not construct, such as from a factory, an instance or an explicit registration, or its rules changed after construction. Remove the registration so Arc constructs the validator. See Arc#2831.");
}
