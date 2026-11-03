// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_QueryOptionsValidator.when_validating;

public class an_origin_with_a_path : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = new ArcOptions();
        options.Query.AllowedWebSocketOrigins = ["https://app.test/path"];

        _result = new QueryOptionsValidator().Validate(null, options);
    }

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
}
