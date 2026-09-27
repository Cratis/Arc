// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Records which self-bindings Arc's convention registration added, so a protected command cannot silently
/// replace an application-supplied validator factory or instance with a different validator.
/// </summary>
/// <param name="services">The registrations of the Arc provider.</param>
/// <param name="conventionBindings">The descriptors Arc itself added by convention.</param>
internal sealed class ValidatorRegistrationProvenance(IServiceCollection services, IReadOnlySet<ServiceDescriptor> conventionBindings)
{
    /// <summary>Rejects registrations whose origin cannot be certified before resolving the validator.</summary>
    /// <param name="validatorType">The discovered validator type.</param>
    /// <exception cref="InvalidOperationException">The validator has explicit or duplicate registrations.</exception>
    internal void Validate(Type validatorType)
    {
        var registrations = services.Where(_ => _.ServiceType == validatorType).ToArray();
        if (registrations.Length == 0 || (registrations.Length == 1 && conventionBindings.Contains(registrations[0]))) return;

        throw new InvalidOperationException($"Registered validator '{validatorType}' cannot run in a protected decision command; only Arc convention self-bindings are supported.");
    }
}
