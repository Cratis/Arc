// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_releasing_a_read_model;

public class and_an_awaited_release_faults : given.a_read_model_resolution
{
    InvalidOperationException _failure;

    void Establish()
    {
        _failure = new InvalidOperationException("release failure");
        GivenSubject();
        GivenReadModelExists();
        _readModels.Release(_readModel).Returns(Task.FromException<TestReadModel>(_failure));
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Microsoft.Extensions.DependencyInjection.ReadModelServiceCollectionExtensions.ResolveReadModelAsync(typeof(TestReadModel), _commandContext, _readModels));

    [Fact] void should_preserve_the_failure_type() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_the_model_name() => _exception.Message.ShouldEqual($"Failed to release read model '{typeof(TestReadModel).FullName}'.");
    [Fact] void should_preserve_the_original_cause() => _exception.InnerException.ShouldBeSame(_failure);
    [Fact] void should_release_exactly_once() => ShouldHaveReleasedReadModel();
}
