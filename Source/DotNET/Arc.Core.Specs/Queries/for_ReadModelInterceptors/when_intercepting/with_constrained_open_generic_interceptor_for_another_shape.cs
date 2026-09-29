// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_constrained_open_generic_interceptor_for_another_shape : given.a_read_model_interceptors
{
    public class ConstrainedListInterceptor<TReadModel> : IInterceptReadModel<List<TReadModel>>
        where TReadModel : OtherReadModel
    {
        public Task<List<TReadModel>> Intercept(List<TReadModel> readModel) => Task.FromResult(readModel);
    }

    Exception _exception;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(ConstrainedListInterceptor<>)]);
        _serviceProvider = Substitute.For<IServiceProvider>();

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _exception = await Catch.Exception(
        () => _interceptors.Intercept(typeof(OtherReadModel), [new OtherReadModel(1)], _serviceProvider));

    [Fact] void should_throw_unsupported_shape_exception() => _exception.ShouldBeOfExactType<OpenGenericReadModelInterceptorMustInterceptItsTypeParameter>();
    [Fact] void should_name_the_interceptor_type() => _exception.Message.ShouldContain(nameof(ConstrainedListInterceptor<>));
}
