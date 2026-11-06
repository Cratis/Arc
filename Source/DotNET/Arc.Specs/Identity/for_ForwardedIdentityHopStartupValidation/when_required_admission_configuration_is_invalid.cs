// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHopStartupValidation;

public class when_required_admission_configuration_is_invalid : given.a_startup_validation
{
    Exception _failure;

    void Establish() => _options.ForwardedIdentity.ClientCertificate.Required = true;

    async Task Because() => _failure = await Catch.Exception(async () => await _validation.StartAsync(CancellationToken.None));

    [Fact] void should_fail_startup() => _failure.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_not_log_the_post_validation_warning() => Warnings.ShouldBeEmpty();
}
