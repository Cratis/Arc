// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_constrained_open_generic_interceptor : given.a_read_model_interceptors
{
    public class ConstrainedInterceptor<TReadModel> : IInterceptReadModel<TReadModel>
        where TReadModel : OtherReadModel
    {
        public List<TReadModel> InterceptedItems { get; } = [];

        public Task<TReadModel> Intercept(TReadModel readModel)
        {
            InterceptedItems.Add(readModel);
            return Task.FromResult(readModel);
        }
    }

    ConstrainedInterceptor<OtherReadModel> _interceptorInstance;
    TestReadModel _unsatisfiedItem;
    OtherReadModel _satisfiedItem;
    IEnumerable<object> _unsatisfiedItems;
    IEnumerable<object> _unsatisfiedResult;
    IEnumerable<object> _satisfiedResult;

    void Establish()
    {
        _unsatisfiedItem = new TestReadModel("hello");
        _satisfiedItem = new OtherReadModel(42);
        _unsatisfiedItems = [_unsatisfiedItem];
        _interceptorInstance = new ConstrainedInterceptor<OtherReadModel>();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(ConstrainedInterceptor<>)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(ConstrainedInterceptor<OtherReadModel>)).Returns(_interceptorInstance);

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because()
    {
        _unsatisfiedResult = await _interceptors.Intercept(typeof(TestReadModel), _unsatisfiedItems, _serviceProvider);
        _satisfiedResult = await _interceptors.Intercept(typeof(OtherReadModel), [_satisfiedItem], _serviceProvider);
    }

    [Fact] void should_return_the_items_untouched_for_the_read_model_not_satisfying_the_constraint() => _unsatisfiedResult.ShouldBeSame(_unsatisfiedItems);
    [Fact] void should_intercept_the_read_model_satisfying_the_constraint() => _interceptorInstance.InterceptedItems.ShouldContain(_satisfiedItem);
    [Fact] void should_intercept_the_satisfying_item_once() => _interceptorInstance.InterceptedItems.Count.ShouldEqual(1);
    [Fact] void should_return_the_satisfying_item() => _satisfiedResult.ShouldContain(_satisfiedItem);
}
