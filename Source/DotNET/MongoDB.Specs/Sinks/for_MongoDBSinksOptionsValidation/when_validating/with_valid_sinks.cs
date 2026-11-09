// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinksOptionsValidation.when_validating;

public class with_valid_sinks : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new MongoDBSinksOptionsValidation().Validate(null, new()
    {
        Sinks = { ["reporting"] = new() { Server = "mongodb://reporting:27017" } }
    });

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
