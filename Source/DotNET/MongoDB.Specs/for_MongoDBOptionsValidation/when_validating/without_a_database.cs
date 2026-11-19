// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_MongoDBOptionsValidation.when_validating;

public class without_a_database : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new MongoDBOptionsValidation("Cratis:MongoDB").Validate(null, new() { Server = "mongodb://localhost:27017" });

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_configuration_key() => _result.FailureMessage.ShouldContain("Cratis:MongoDB:Database");
    [Fact] void should_name_the_environment_variable() => _result.FailureMessage.ShouldContain("Cratis__MongoDB__Database");
}
