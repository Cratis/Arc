// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_a_scoped_credential_verifier_is_registered : given.a_capability_startup_validation
{
    IVerifiedCredentialSource _source;

    void Establish()
    {
        _source = Substitute.For<VerifiedCredentialSourceForSpecs, IDisposable>();
        _services.Clear();
        _services.AddScoped(_ => _source);
    }

    Task Because() => Validate();

    [Fact] void should_allow_startup() => _result.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_dispose_the_scope() => ((IDisposable)_source).Received(1).Dispose();
}
