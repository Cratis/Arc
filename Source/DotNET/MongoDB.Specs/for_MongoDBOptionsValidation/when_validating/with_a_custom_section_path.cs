// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_MongoDBOptionsValidation.when_validating;

public class with_a_custom_section_path : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new MongoDBOptionsValidation("Shop:Documents").Validate(null, new());

    [Fact] void should_name_the_server_key_under_the_custom_path() => _result.FailureMessage.ShouldContain("Shop:Documents:Server");
    [Fact] void should_name_the_database_environment_variable_under_the_custom_path() => _result.FailureMessage.ShouldContain("Shop__Documents__Database");
    [Fact] void should_not_name_the_default_path() => _result.FailureMessage.ShouldNotContain("Cratis:MongoDB");
}
