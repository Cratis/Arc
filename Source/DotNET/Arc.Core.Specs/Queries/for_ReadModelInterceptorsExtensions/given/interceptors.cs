// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptorsExtensions.given;

public class interceptors : Specification
{
    protected IReadModelInterceptors _interceptors;
    protected IServiceProvider _serviceProvider;

    public record TestReadModel(string Value);

    void Establish()
    {
        _interceptors = Substitute.For<IReadModelInterceptors>();
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));
        _serviceProvider = Substitute.For<IServiceProvider>();
    }
}
