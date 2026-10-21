// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

[Collection("UsesCurrentDirectory")]
public class when_a_body_cannot_be_bound : Specification
{
    readonly List<(string Scenario, int Status, JsonElement Body)> _results = [];
    int _markedCalls;
    int _dependentCalls;
    int _assertionCalls;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("BindingClaims", policy => policy.RequireClaim("permission", "write"))
            .AddPolicy("BindingMarked", policy => policy.AddRequirements(new MarkedRequirement()))
            .AddPolicy("BindingDependent", policy => policy.AddRequirements(new DependentRequirement()))
            .AddPolicy("BindingAssertion", policy => policy.RequireAssertion(context =>
            {
                _assertionCalls++;
                return context.Resource is not null;
            }));
        builder.Services.AddSingleton<IAuthorizationHandler>(new MarkedRequirementHandler(() => _markedCalls++));
        builder.Services.AddSingleton<IAuthorizationHandler>(new DependentRequirementHandler(() => _dependentCalls++));
        var types = new[] { typeof(ClaimsCommand), typeof(MarkedCommand), typeof(DependentCommand), typeof(AssertionCommand), typeof(AnonymousCommand), typeof(RoleCommand) };
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var commandHandlers = types.Select(type =>
        {
            var handler = Substitute.For<ICommandHandler>();
            handler.CommandType.Returns(type);
            handler.Location.Returns(["Binding", type.Name]);
            return handler;
        }).ToList();
        handlers.Handlers.Returns(commandHandlers);
        builder.Services.AddSingleton(handlers);
        await using var app = builder.Build();
        var mapper = Substitute.For<IEndpointMapper>();
        var endpoints = new List<(string Name, Func<IHttpRequestContext, Task> Handler)>();
        mapper.When(value => value.MapPost(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(call => endpoints.Add((call.ArgAt<EndpointMetadata>(2).Name, call.ArgAt<Func<IHttpRequestContext, Task>>(1))));
        mapper.MapCommandEndpoints(app.Services);

        foreach (var type in types)
        {
            foreach (var caller in new[] { "anonymous", "authenticated", "allowed" })
            {
                foreach (var endpoint in endpoints.Where(endpoint => endpoint.Name.EndsWith(type.FullName!, StringComparison.Ordinal)))
                {
                    await using var scope = app.Services.CreateAsyncScope();
                    var principal = caller == "anonymous"
                        ? new ClaimsPrincipal(new ClaimsIdentity())
                        : new ClaimsPrincipal(new ClaimsIdentity(caller == "allowed" ? [new Claim("permission", "write"), new Claim(ClaimTypes.Role, "Editor")] : [], "test"));
                    var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal };
                    context.Request.ContentType = "application/json";
                    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"from":"2026-10-16T12:30"}"""));
                    context.Response.Body = new MemoryStream();
                    scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
                    using var identity = scope.ServiceProvider.GetRequiredService<ICurrentPrincipalOverride>().BeginScope(principal);
                    await endpoint.Handler(new AspNetCoreHttpRequestContext(context));
                    var body = JsonDocument.Parse(((MemoryStream)context.Response.Body).ToArray());
                    _results.Add(($"{type.Name}:{caller}", context.Response.StatusCode, body.RootElement.Clone()));
                    body.Dispose();
                }
            }
        }
    }

    [Fact] void should_cover_execute_and_validate_for_every_caller() => _results.Count.ShouldEqual(36);
    [Fact] void should_deny_an_anonymous_claim_policy_caller() => Statuses("ClaimsCommand:anonymous").ShouldContainOnly(403, 403);
    [Fact] void should_deny_an_authenticated_caller_missing_the_claim() => Statuses("ClaimsCommand:authenticated").ShouldContainOnly(403, 403);
    [Fact] void should_report_validation_for_the_allowed_claim_policy_caller() => Statuses("ClaimsCommand:allowed").ShouldContainOnly(400, 400);
    [Fact] void should_deny_an_anonymous_marked_policy_caller() => Statuses("MarkedCommand:anonymous").ShouldContainOnly(403, 403);
    [Fact] void should_deny_an_anonymous_resource_dependent_policy_caller() => Statuses("DependentCommand:anonymous").ShouldContainOnly(403, 403);
    [Fact] void should_deny_an_anonymous_assertion_policy_caller() => Statuses("AssertionCommand:anonymous").ShouldContainOnly(403, 403);
    [Fact] void should_report_the_explicit_offset_message() => _results.Where(result => result.Status == 400).All(result => result.Body.GetProperty("validationResults")[0].GetProperty("message").GetString() == "DateTime command property 'from' requires an explicit offset or Z.").ShouldBeTrue();
    [Fact] void should_evaluate_marked_requirements() => _markedCalls.ShouldEqual(4);
    [Fact] void should_report_validation_after_a_marked_policy_allows() => Statuses("MarkedCommand:allowed").ShouldContainOnly(400, 400);
    [Fact] void should_not_evaluate_unmarked_requirements() => _dependentCalls.ShouldEqual(0);
    [Fact] void should_report_validation_for_a_resource_dependent_policy() => Statuses("DependentCommand:authenticated").ShouldContainOnly(400, 400);
    [Fact] void should_not_evaluate_assertion_requirements() => _assertionCalls.ShouldEqual(0);
    [Fact] void should_report_validation_for_an_assertion_policy() => Statuses("AssertionCommand:authenticated").ShouldContainOnly(400, 400);
    [Fact] void should_allow_an_anonymous_command_to_report_validation() => Statuses("AnonymousCommand:anonymous").ShouldContainOnly(400, 400);
    [Fact] void should_refuse_a_caller_without_the_role() => Statuses("RoleCommand:authenticated").ShouldContainOnly(403, 403);
    [Fact] void should_report_the_member_on_every_binding_failure() => _results.Where(result => result.Status == 400).All(result => result.Body.GetProperty("validationResults")[0].GetProperty("members")[0].GetString() == "from").ShouldBeTrue();
    [Fact] void should_not_disclose_validation_on_denials() => _results.Where(result => result.Status == 403).All(result => result.Body.GetProperty("validationResults").GetArrayLength() == 0).ShouldBeTrue();

    int[] Statuses(string scenario) => _results.Where(result => result.Scenario == scenario).Select(result => result.Status).ToArray();

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "BindingClaims")]
    public record ClaimsCommand([property: ExplicitOffset] DateTimeOffset From);

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "BindingMarked")]
    public record MarkedCommand([property: ExplicitOffset] DateTimeOffset From);

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "BindingDependent")]
    public record DependentCommand([property: ExplicitOffset] DateTimeOffset From);

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "BindingAssertion")]
    public record AssertionCommand([property: ExplicitOffset] DateTimeOffset From);

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public record AnonymousCommand([property: ExplicitOffset] DateTimeOffset From);

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Editor")]
    public record RoleCommand([property: ExplicitOffset] DateTimeOffset From);

    public class MarkedRequirement : IResourceFreeRequirement;
    public class DependentRequirement : IAuthorizationRequirement;

    class MarkedRequirementHandler(Action onCall) : AuthorizationHandler<MarkedRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MarkedRequirement requirement)
        {
            context.Resource.ShouldBeNull();
            onCall();
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    class DependentRequirementHandler(Action onCall) : AuthorizationHandler<DependentRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DependentRequirement requirement)
        {
            onCall();
            return Task.CompletedTask;
        }
    }
}
