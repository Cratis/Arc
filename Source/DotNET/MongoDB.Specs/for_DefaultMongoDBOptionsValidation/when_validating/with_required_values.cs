// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_DefaultMongoDBOptionsValidation.when_validating;

public class with_required_values : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new DefaultMongoDBOptionsValidation().Validate(null, new MongoDBOptions
    {
        Server = "mongodb://localhost:27017",
        Database = "db"
    });

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
