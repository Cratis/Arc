// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.given;

public class an_arc_builder : Specification
{
    protected IServiceCollection _services;
    protected ITypes _types;
    protected IArcBuilder _builder;

    void Establish()
    {
        _services = new ServiceCollection();
        _types = Substitute.For<ITypes>();
        _types.All.Returns([]);
        _types.FindMultiple<object>().ReturnsForAnyArgs([]);
        _builder = new ArcBuilder(_services, _types);
    }
}
