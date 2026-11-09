// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinksOptionsValidation.when_validating;

public class with_both_server_and_server_file : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new MongoDBSinksOptionsValidation().Validate(null, new()
    {
        Sinks = { ["reporting"] = new() { Server = "mongodb://reporting:27017", ServerFile = "/var/run/secrets/reporting" } }
    });

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_both_settings() => _result.FailureMessage.ShouldContain("'Sinks:reporting:Server' and 'Sinks:reporting:ServerFile'");
}
