// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_nesting_a_no_scheme_core_pipeline_under_another_actor : Specification
{
    CommandResult _result;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(NestedCoreParent), typeof(NestedCoreChild)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(call => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        await using var app = builder.Build();
        using var outer = app.Services.GetRequiredService<ISystemExecution>().As(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "parent")], "test")));
        await using var services = app.Services.CreateAsyncScope();
        _result = await app.Services.GetRequiredService<ICommandPipeline>().Execute(new NestedCoreParent(), services.ServiceProvider);
    }

    [Fact] void should_authorize_the_outer_command() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_report_a_nested_pipeline_error() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_authorize_the_inner_command() => NestedCoreParent.ChildAuthorized.ShouldBeTrue();
    [Fact] void should_handle_the_inner_command_as_the_second_actor() => NestedCoreChild.HandlerActor.ShouldEqual("child");
    [Fact] void should_restore_the_outer_actor_after_the_nested_command() => NestedCoreParent.RestoredActor.ShouldEqual("parent");
}
