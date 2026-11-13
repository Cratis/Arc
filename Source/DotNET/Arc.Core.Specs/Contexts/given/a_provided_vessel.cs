// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts.given;

public class a_provided_vessel : Specification
{
    protected IContextValueSource _source;
    protected ServiceProvider _services;
    protected Type _readFor;

    public record VesselValues(string Vessel, int? Draft);

    void Establish()
    {
        _source = Substitute.For<IContextValueSource>();
        _source.Read(Arg.Any<Type?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _readFor = call.Arg<Type?>();
            return call.Arg<string>() switch
            {
                "vessel" => ValueTask.FromResult(ContextValueRead.Present("Aurora")),
                "draft" => ValueTask.FromResult(ContextValueRead.Present(12)),
                _ => ValueTask.FromResult(ContextValueRead.Absent)
            };
        });
        _services = new ServiceCollection().AddSingleton(_source).BuildServiceProvider();
    }

    void Destroy() => _services.Dispose();
}
