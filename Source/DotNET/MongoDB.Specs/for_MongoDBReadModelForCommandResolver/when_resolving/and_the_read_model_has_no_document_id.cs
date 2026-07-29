// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving;

public class and_the_read_model_has_no_document_id : given.a_resolver
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _resolver.Resolve(typeof(Preferences), CommandContextWith<Preferences>(Guid.NewGuid().ToString())));

    [Fact] void should_fail_naming_the_missing_id_mapping() => _exception.ShouldBeOfExactType<MissingIdMapping>();
}
