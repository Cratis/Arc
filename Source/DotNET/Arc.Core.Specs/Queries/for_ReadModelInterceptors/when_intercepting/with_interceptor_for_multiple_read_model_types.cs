// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_interceptor_for_multiple_read_model_types : given.a_read_model_interceptors
{
    public class MultipleReadModelInterceptor : IInterceptReadModel<TestReadModel>, IInterceptReadModel<OtherReadModel>
    {
        public List<object> InterceptedItems { get; } = [];

        public Task<TestReadModel> Intercept(TestReadModel readModel)
        {
            InterceptedItems.Add(readModel);
            return Task.FromResult(readModel);
        }

        public Task<OtherReadModel> Intercept(OtherReadModel readModel)
        {
            InterceptedItems.Add(readModel);
            return Task.FromResult(readModel);
        }
    }

    MultipleReadModelInterceptor _interceptorInstance;
    TestReadModel _testItem;
    OtherReadModel _otherItem;

    void Establish()
    {
        _testItem = new TestReadModel("hello");
        _otherItem = new OtherReadModel(42);
        _interceptorInstance = new MultipleReadModelInterceptor();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(MultipleReadModelInterceptor)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(MultipleReadModelInterceptor)).Returns(_interceptorInstance);

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because()
    {
        await _interceptors.Intercept(typeof(TestReadModel), [_testItem], _serviceProvider);
        await _interceptors.Intercept(typeof(OtherReadModel), [_otherItem], _serviceProvider);
    }

    [Fact] void should_intercept_both_read_models() => _interceptorInstance.InterceptedItems.ShouldContainOnly([_testItem, _otherItem]);
}
