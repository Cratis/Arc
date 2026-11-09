// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinksOptionsValidation.when_validating;

public class with_a_missing_server_file : Specification
{
    string _path;
    ValidateOptionsResult _result;

    void Establish() => _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    void Because() => _result = new MongoDBSinksOptionsValidation().Validate(null, new()
    {
        Sinks = { ["reporting"] = new() { ServerFile = _path } }
    });

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_setting_and_the_path() => _result.FailureMessage.ShouldContain($"'Sinks:reporting:ServerFile' names '{_path}'");
}
