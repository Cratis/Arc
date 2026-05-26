// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class it_should_register_library_activity_sources : Specification
{
    IActivitySource<CommandPipeline> _commandPipelineActivitySource;
    IActivitySource<CommandFilters> _commandFiltersActivitySource;
    IActivitySource<QueryPipeline> _queryPipelineActivitySource;
    IActivitySource<QueryFilters> _queryFiltersActivitySource;
    IActivitySource<IdentityProvider> _identityProviderActivitySource;

    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _commandPipelineActivitySource = serviceProvider.GetRequiredService<IActivitySource<CommandPipeline>>();
        _commandFiltersActivitySource = serviceProvider.GetRequiredService<IActivitySource<CommandFilters>>();
        _queryPipelineActivitySource = serviceProvider.GetRequiredService<IActivitySource<QueryPipeline>>();
        _queryFiltersActivitySource = serviceProvider.GetRequiredService<IActivitySource<QueryFilters>>();
        _identityProviderActivitySource = serviceProvider.GetRequiredService<IActivitySource<IdentityProvider>>();
    }

    [Fact] void should_use_the_arc_activity_source_name_for_command_pipeline() => _commandPipelineActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_use_the_arc_activity_source_name_for_command_filters() => _commandFiltersActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_use_the_arc_activity_source_name_for_query_pipeline() => _queryPipelineActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_use_the_arc_activity_source_name_for_query_filters() => _queryFiltersActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_use_the_arc_activity_source_name_for_identity_provider() => _identityProviderActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_share_the_same_activity_source_instance() => ReferenceEquals(_commandPipelineActivitySource.ActualSource, _queryPipelineActivitySource.ActualSource).ShouldBeTrue();
}
