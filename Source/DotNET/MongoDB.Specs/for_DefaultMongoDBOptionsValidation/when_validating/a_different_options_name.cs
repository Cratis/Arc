// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_DefaultMongoDBOptionsValidation.when_validating;

public class a_different_options_name : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new DefaultMongoDBOptionsValidation().Validate("other", new MongoDBOptions());

    [Fact] void should_skip_validation() => _result.Skipped.ShouldBeTrue();
}
