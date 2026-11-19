// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryOptionsValidator.when_validating;

public class a_capacity_of_one : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = new ArcOptions();
        options.Query.MaxQueuedEmissionsPerSubscription = 1;

        _result = new QueryOptionsValidator().Validate(null, options);
    }

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
