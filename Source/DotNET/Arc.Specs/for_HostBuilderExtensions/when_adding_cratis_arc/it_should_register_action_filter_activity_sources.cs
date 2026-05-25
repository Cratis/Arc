// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions.when_adding_cratis_arc;

public class it_should_register_action_filter_activity_sources : Specification
{
    IActivitySource<CommandActionFilter> _commandActionFilterActivitySource;
    IActivitySource<QueryActionFilter> _queryActionFilterActivitySource;
    IActivitySource<CommandPipeline> _commandPipelineActivitySource;

    void Because()
    {
        using var host = new HostBuilder()
            .AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider))
            .Build();

        _commandActionFilterActivitySource = host.Services.GetRequiredKeyedService<IActivitySource<CommandActionFilter>>(Internals.ActivitySourceName);
        _queryActionFilterActivitySource = host.Services.GetRequiredKeyedService<IActivitySource<QueryActionFilter>>(Internals.ActivitySourceName);
        _commandPipelineActivitySource = host.Services.GetRequiredKeyedService<IActivitySource<CommandPipeline>>(Internals.ActivitySourceName);
    }

    [Fact] void should_use_the_arc_activity_source_name_for_command_action_filter() => _commandActionFilterActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_use_the_arc_activity_source_name_for_query_action_filter() => _queryActionFilterActivitySource.ActualSource.Name.ShouldEqual("Cratis.Arc");
    [Fact] void should_share_the_same_activity_source_instance_as_core_services() => ReferenceEquals(_commandActionFilterActivitySource.ActualSource, _commandPipelineActivitySource.ActualSource).ShouldBeTrue();
}
