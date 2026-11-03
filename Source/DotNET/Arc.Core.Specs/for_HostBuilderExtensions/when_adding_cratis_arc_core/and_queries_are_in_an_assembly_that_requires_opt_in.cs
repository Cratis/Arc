// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.for_HostBuilderExtensions.given;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class and_queries_are_in_an_assembly_that_requires_opt_in : Specification
{
    ServiceProvider _withoutOptIn;
    ServiceProvider _withOptIn;
    Type _readModel;
    IEnumerable<Type> _withoutOptInReadModels;
    IEnumerable<Type> _withOptInReadModels;

    void Establish()
    {
        var marked = MarkedArtifactHosts.CreateMarkedAssembly();
        _readModel = marked.ReadModel;
        _withoutOptIn = MarkedArtifactHosts.Build([marked.ReadModel], null);
        _withOptIn = MarkedArtifactHosts.Build([marked.ReadModel], marked.Assembly);
    }

    void Because()
    {
        _withoutOptInReadModels = _withoutOptIn.GetRequiredService<IQueryPerformerProviders>().Performers.Select(_ => _.ReadModelType);
        _withOptInReadModels = _withOptIn.GetRequiredService<IQueryPerformerProviders>().Performers.Select(_ => _.ReadModelType);
    }

    [Fact] void should_not_serve_the_query_when_the_assembly_is_not_included() => _withoutOptInReadModels.ShouldNotContain(_readModel);
    [Fact] void should_serve_the_query_when_the_assembly_is_included() => _withOptInReadModels.ShouldContain(_readModel);

    void Destroy()
    {
        _withoutOptIn.Dispose();
        _withOptIn.Dispose();
    }
}
