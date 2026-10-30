// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_releasing_without_dynamic_code;

public class and_release_faults : given.a_read_model_resolution
{
    InvalidOperationException _failure;

    void Establish()
    {
        _failure = new InvalidOperationException("release failure");
        GivenReadModelExists();
        _readModels.Release(_readModel).Returns(Task.FromException<TestReadModel>(_failure));
    }

    void Because() => _exception = Catch.Exception(() =>
        Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ReleaseReadModelWithoutDynamicCode(_readModels, typeof(TestReadModel), _readModel));

    [Fact] void should_propagate_the_original_exception() => _exception.ShouldEqual(_failure);
}
