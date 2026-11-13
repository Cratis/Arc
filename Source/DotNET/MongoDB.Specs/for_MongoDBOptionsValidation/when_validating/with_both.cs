// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_MongoDBOptionsValidation.when_validating;

public class with_both : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new MongoDBOptionsValidation("Cratis:MongoDB").Validate(null, new() { Server = "mongodb://localhost:27017", Database = "app" });

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
