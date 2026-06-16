// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

public class without_read_model_and_without_subject : given.a_read_model_resolution
{
    void Establish() => GivenReadModelDoesNotExist();

    void Because() => ResolveReadModel();

    [Fact] void should_return_null() => _result.ShouldBeNull();
    [Fact] void should_not_release_the_read_model() => ShouldNotHaveReleasedReadModel();
}
