// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryOptionsValidator.when_validating;

public class valid_origins : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = new ArcOptions();
        foreach (var origin in new[] { "https://app.test", "http://localhost:5173/" })
        {
            options.Query.AllowedWebSocketOrigins.Add(origin);
        }

        _result = new QueryOptionsValidator().Validate(null, options);
    }

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
