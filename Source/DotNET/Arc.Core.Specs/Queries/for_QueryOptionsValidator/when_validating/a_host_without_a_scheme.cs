// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryOptionsValidator.when_validating;

public class a_host_without_a_scheme : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = new ArcOptions();
        foreach (var origin in new[] { "app.test" })
        {
            options.Query.AllowedWebSocketOrigins.Add(origin);
        }

        _result = new QueryOptionsValidator().Validate(null, options);
    }

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
}
