// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Constructs invocation-owned validators using evidence tied to the actual executing native scope.
/// </summary>
internal static class CertifiedProtectedValidatorConstruction
{
    static readonly ConditionalWeakTable<object, Stamp> _stamps = new();

    /// <summary>
    /// Determines whether the root selected Arc's new configured-validator registration.
    /// </summary>
    /// <param name="provider">The executing provider.</param>
    /// <param name="validatorType">The validator type.</param>
    /// <returns>Whether to use fresh configured construction instead of the existing parameterless certificate.</returns>
    internal static bool IsConfigured(IServiceProvider provider, Type validatorType) =>
        ProtectedDecisionProviderAttestations.TryGetAttestation(provider, out var scope) &&
        scope.Root.Manifest.BindingsFor(validatorType).Any(binding => scope.Root.Manifest.OriginOf(binding)?.Configure is not null);

    /// <summary>
    /// Preflights and activates a fresh validator under the executing command policy.
    /// </summary>
    /// <param name="provider">The executing provider.</param>
    /// <param name="validatorType">The validator to construct.</param>
    /// <param name="dynamicCodeSupported">The runtime capability, with an internal unsupported-runtime spec seam.</param>
    /// <returns>The fresh, stamped validator.</returns>
    /// <exception cref="DiscoverableValidatorRefusedInProtectedDecision">The runtime, scope or invocation is ineligible.</exception>
    internal static object Construct(IServiceProvider provider, Type validatorType, bool dynamicCodeSupported)
    {
        if (!dynamicCodeSupported || !ProtectedDecisionProviderAttestations.TryGetAttestation(provider, out var scope) ||
            ProtectedValidatorActivationFrame.Current is not { Completed: false } frame ||
            CommandDecisionPolicy.Token is not { } token || !ReferenceEquals(frame.Token, token))
        {
            throw DiscoverableValidatorRefusedInProtectedDecision.NotConstructedByArc(validatorType);
        }
        var planner = new ProtectedValidatorDependencyPlan(scope.Root.Manifest, validatorType);
        var plan = planner.Build();
        planner.ResolveReads(provider);
        var validator = plan.Activate(frame);
        ProtectedValidatorConstruction.Certify(validator);
        _stamps.Add(validator, new(scope, frame, token, validatorType));
        return validator;
    }

    /// <summary>
    /// Rechecks invocation, scope liveness and post-callback rule identities before invoking a new-path validator.
    /// </summary>
    /// <param name="validator">The validator about to run.</param>
    /// <param name="provider">The actual provider of this model-validation request.</param>
    /// <exception cref="DiscoverableValidatorRefusedInProtectedDecision">The instance no longer belongs to this scope and invocation or its rules changed.</exception>
    internal static void VerifyInvocation(object validator, IServiceProvider? provider)
    {
        if (!_stamps.TryGetValue(validator, out var stamp))
        {
            return;
        }
        if (!ReferenceEquals(provider, stamp.Scope.Provider) || stamp.Frame.Completed || !ReferenceEquals(ProtectedValidatorActivationFrame.Current, stamp.Frame) ||
            !ReferenceEquals(CommandDecisionPolicy.Token, stamp.Token) ||
            !ProtectedDecisionProviderAttestations.TryGetAttestation(stamp.Scope.Provider, out var scope) ||
            !ReferenceEquals(scope, stamp.Scope) || !ProtectedValidatorConstruction.IsCertified(validator, stamp.Type))
        {
            throw DiscoverableValidatorRefusedInProtectedDecision.NotConstructedByArc(stamp.Type);
        }
    }

    sealed record Stamp(ProtectedDecisionScopeAttestation Scope, ProtectedValidatorActivationFrame Frame, object Token, Type Type);
}
