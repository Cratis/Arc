// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_interceptor_that_throws_synchronously : given.a_read_model_interceptors
{
    public class SynchronouslyThrowingInterceptor : IInterceptReadModel<TestReadModel>
    {
        /// <inheritdoc/>
        /// <remarks>Deliberately not async: the exception is thrown from the call itself rather than captured in the returned task.</remarks>
        public Task<TestReadModel> Intercept(TestReadModel readModel) => throw new InvalidOperationException("Interceptor failed");
    }

    Exception _exception;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(SynchronouslyThrowingInterceptor)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(SynchronouslyThrowingInterceptor)).Returns(new SynchronouslyThrowingInterceptor());

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _exception = await Catch.Exception(
        () => _interceptors.Intercept(typeof(TestReadModel), [new TestReadModel("hello")], _serviceProvider));

    [Fact] void should_throw_the_interceptor_exception() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_wrap_the_exception() => (_exception is TargetInvocationException).ShouldBeFalse();
    [Fact] void should_keep_the_message() => _exception.Message.ShouldEqual("Interceptor failed");
}
