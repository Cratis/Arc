// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

public class without_subject : given.a_read_model_resolution
{
    void Establish() => GivenReadModelExists();

    void Because() => ResolveReadModel();

    [Fact] void should_return_the_loaded_read_model() => _result.ShouldEqual(_readModel);
    [Fact] void should_not_release_the_read_model() => ShouldNotHaveReleasedReadModel();
}
