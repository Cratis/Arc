// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_read_model;

public class with_subject_in_response : given.a_read_model_resolution
{
    void Establish()
    {
        GivenSubjectInResponse();
        GivenReadModelExists();
        GivenReadModelReleases();
    }

    void Because() => ResolveReadModel();

    [Fact] void should_return_the_released_read_model() => _result.ShouldEqual(_releasedReadModel);
    [Fact] void should_release_the_read_model() => ShouldHaveReleasedReadModel();
}
