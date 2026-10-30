// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_releasing_without_dynamic_code;

public class and_release_returns_the_read_model : given.a_read_model_resolution
{
    object _released;

    void Establish()
    {
        GivenReadModelExists();
        GivenReadModelReleases();
    }

    void Because() => _released = Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ReleaseReadModelWithoutDynamicCode(_readModels, typeof(TestReadModel), _readModel);

    [Fact] void should_return_what_release_returned() => _released.ShouldEqual(_releasedReadModel);
    [Fact] void should_release_the_read_model() => ShouldHaveReleasedReadModel();
}
