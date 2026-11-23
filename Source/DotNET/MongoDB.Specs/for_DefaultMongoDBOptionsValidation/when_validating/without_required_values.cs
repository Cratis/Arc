// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_DefaultMongoDBOptionsValidation.when_validating;

public class without_required_values : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new DefaultMongoDBOptionsValidation().Validate(Options.DefaultName, new MongoDBOptions());

    [Fact] void should_fail_validation() => _result.Failed.ShouldBeTrue();

    [Fact] void should_report_two_failures() => _result.Failures.Count().ShouldEqual(2);

    [Fact] void should_report_missing_server() => _result.Failures.ShouldContain(_ => _.StartsWith("Server: ", StringComparison.Ordinal));

    [Fact] void should_report_missing_database() => _result.Failures.ShouldContain(_ => _.StartsWith("Database: ", StringComparison.Ordinal));
}
