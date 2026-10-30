// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_releasing_a_read_model;

public class and_release_faults : given.a_read_model_resolution
{
    InvalidOperationException _failure;

    void Establish()
    {
        _failure = new InvalidOperationException("release failure");
        GivenSubject();
        GivenReadModelExists();
        _readModels.Release(_readModel).Returns(Task.FromException<TestReadModel>(_failure));
    }

    void Because() => CatchResolveReadModelException();

    [Fact] void should_throw_an_invalid_operation_exception() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_read_model() => _exception.Message.ShouldEqual($"Failed to release read model '{typeof(TestReadModel).FullName}'.");
    [Fact] void should_carry_the_original_exception() => _exception.InnerException.ShouldEqual(_failure);
}
