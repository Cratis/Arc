// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Validates the discovered command and query authorization catalog before a listener accepts traffic.
/// </summary>
/// <param name="handlers">Discovered command handlers.</param>
/// <param name="performers">Discovered query performers.</param>
/// <param name="scopeFactory">Creates a temporary validation scope.</param>
public class AuthorizationConfigurationValidator(
    ICommandHandlerProviders handlers,
    IQueryPerformerProviders performers,
    IServiceScopeFactory scopeFactory)
{
    /// <summary>
    /// Validates every discovered authorization declaration.
    /// </summary>
    /// <param name="cancellationToken">Startup cancellation token.</param>
    /// <returns>The validation operation.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A discovered declaration cannot be validated safely.</exception>
    public async Task Validate(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var declarations = scope.ServiceProvider.GetRequiredService<AuthorizationDeclarations>();
        var runtime = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyRuntime>();
        if (scope.ServiceProvider.GetServices<AnonymousAspNetAuthorizationPolicyRegistration>().Any())
        {
            if (runtime is ArcAuthorizationPolicyRuntime)
            {
                throw new InvalidAuthorizationConfiguration("ASP.NET Core anonymous policy opt-ins require the ASP.NET Core Arc host.");
            }

            await runtime.Validate([], scope.ServiceProvider, cancellationToken);
        }
        foreach (var handler in handlers.Handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await runtime.Validate(declarations.For(handler.CommandType).Requirements, scope.ServiceProvider, cancellationToken);
        }

        foreach (var performer in performers.Performers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = QueryAuthorizationTarget.For(performer, declarations);
            var declaration = target switch
            {
                System.Reflection.MethodInfo method => declarations.For(method),
                Type type => declarations.For(type),
                _ => throw new InvalidAuthorizationConfiguration($"Unsupported authorization target '{target}'.")
            };
            await runtime.Validate(declaration.Requirements, scope.ServiceProvider, cancellationToken);
        }
    }
}
