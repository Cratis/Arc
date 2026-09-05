// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

/// <summary>
/// Reproduces the production failure that motivated skipping streaming results in the query pipeline. When an
/// observable query result — an <see cref="ISubject{T}"/> wrapper such as Arc's MongoDB LifetimeAwareSubject — is
/// handed to the per-item interceptor as if it were a read model, the reflective
/// <c>IInterceptReadModel&lt;TReadModel&gt;.Intercept(TReadModel)</c> call cannot bind the wrapper to the read
/// model type and throws "...cannot be converted to type...". The pipeline must therefore never pass a streaming
/// result to the interceptors.
/// </summary>
public class with_a_streaming_subject_instead_of_a_read_model : given.a_read_model_interceptors
{
    GenericReadModelInterceptor<TestReadModel> _interceptorInstance;
    ISubject<IEnumerable<TestReadModel>> _subject;
    Exception _exception;

    void Establish()
    {
        _subject = new ReplaySubject<IEnumerable<TestReadModel>>();
        _interceptorInstance = new GenericReadModelInterceptor<TestReadModel>();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(GenericReadModelInterceptor<>)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(GenericReadModelInterceptor<TestReadModel>)).Returns(_interceptorInstance);

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _exception = await Catch.Exception(
        () => _interceptors.Intercept(typeof(TestReadModel), [_subject], _serviceProvider));

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_fail_to_convert_the_subject_to_the_read_model_type() =>
        _exception.Message.ShouldContain("cannot be converted to type");
    [Fact] void should_not_intercept_the_subject() => _interceptorInstance.InterceptedItems.ShouldBeEmpty();
}
