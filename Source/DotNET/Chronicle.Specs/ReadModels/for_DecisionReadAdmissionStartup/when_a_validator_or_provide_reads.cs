// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.Logging;
using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_a_validator_or_provide_reads : given.a_startup
{
    void Establish()
    {
        _types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(BalanceValidator)]);
        Configure(typeof(ValidatorCommand), typeof(ProvideCommand));
    }
    Task Because() => StartAndCheck();

    [Fact] void should_check_validator_constructor_reads() => Logged(LogLevel.Critical, typeof(ValidatorCommand).FullName!).ShouldBeTrue();
    [Fact] void should_check_provide_parameters() => Logged(LogLevel.Critical, typeof(ProvideCommand).FullName!).ShouldBeTrue();
    [Fact] void should_cache_the_verdict() => _reader.Received(1).Describe<Balance>();
}
