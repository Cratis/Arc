// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.given;

public class a_read_model_interceptors : Specification
{
    protected ReadModelInterceptors _interceptors;
    protected ITypes _types;
    protected IServiceProvider _serviceProvider;

    protected record TestReadModel(string Value);

    protected class TestReadModelInterceptor : IInterceptReadModel<TestReadModel>
    {
        public List<TestReadModel> InterceptedItems { get; } = [];

        public Task<TestReadModel> Intercept(TestReadModel readModel)
        {
            InterceptedItems.Add(readModel);
            return Task.FromResult(readModel);
        }
    }

    protected class AnotherTestReadModelInterceptor : IInterceptReadModel<TestReadModel>
    {
        public List<TestReadModel> InterceptedItems { get; } = [];

        public Task<TestReadModel> Intercept(TestReadModel readModel)
        {
            InterceptedItems.Add(readModel);
            return Task.FromResult(readModel);
        }
    }

    protected record OtherReadModel(int Count);

    protected class OtherReadModelInterceptor : IInterceptReadModel<OtherReadModel>
    {
        public Task<OtherReadModel> Intercept(OtherReadModel readModel) => Task.FromResult(readModel);
    }
}
