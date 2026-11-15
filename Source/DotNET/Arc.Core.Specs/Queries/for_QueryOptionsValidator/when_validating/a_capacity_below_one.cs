// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryOptionsValidator.when_validating;

public class a_capacity_below_one : Specification
{
    ValidateOptionsResult _zero;
    ValidateOptionsResult _negative;

    void Because()
    {
        var options = new ArcOptions();
        options.Query.MaxQueuedEmissionsPerSubscription = 0;
        _zero = new QueryOptionsValidator().Validate(null, options);

        options.Query.MaxQueuedEmissionsPerSubscription = -1;
        _negative = new QueryOptionsValidator().Validate(null, options);
    }

    [Fact] void should_fail_for_zero() => _zero.Failed.ShouldBeTrue();
    [Fact] void should_fail_for_a_negative_value() => _negative.Failed.ShouldBeTrue();
    [Fact] void should_name_the_setting() => _zero.FailureMessage.ShouldContain("Cratis:Arc:Query:MaxQueuedEmissionsPerSubscription");
}
