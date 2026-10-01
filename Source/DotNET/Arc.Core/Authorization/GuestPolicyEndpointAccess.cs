// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Lets guest policies reach the pipeline without granting an authorization verdict at the HTTP boundary.
/// </summary>
internal static class GuestPolicyEndpointAccess
{
    /// <summary>
    /// Checks whether every effective command requirement opts into guest evaluation.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="services">The host services.</param>
    /// <returns>Whether the HTTP host may defer authentication to the guest policies.</returns>
    internal static bool ForCommand(Type commandType, IServiceProvider services) =>
        Resolve(services, declarations => declarations.For(commandType));

    /// <summary>
    /// Checks the query's effective method or type declaration for guest evaluation.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <param name="services">The host services.</param>
    /// <returns>Whether the HTTP host may defer authentication to the guest policies.</returns>
    internal static bool ForQuery(IQueryPerformer performer, IServiceProvider services) =>
        Resolve(services, declarations => QueryAuthorizationTarget.For(performer, declarations) switch
        {
            System.Reflection.MethodInfo method => declarations.For(method),
            Type type => declarations.For(type),
            _ => throw new InvalidAuthorizationConfiguration($"Unsupported query authorization target '{performer.FullyQualifiedName}'.")
        });

    static bool Resolve(IServiceProvider services, Func<AuthorizationDeclarations, AuthorizationDeclaration> declarationFor)
    {
        using var scope = services.CreateScope();
        var declarations = scope.ServiceProvider.GetService<AuthorizationDeclarations>();
        var runtime = scope.ServiceProvider.GetService<IAuthorizationPolicyRuntime>();
        if (declarations is null || runtime is null)
        {
            return false;
        }

        try
        {
            var declaration = declarationFor(declarations);
            if (!declaration.RequiresAsynchronousEvaluation)
            {
                return false;
            }

            // Resolve only the plan, never select a principal or run a policy. Both hosts must use the
            // same all-requirements opt-in that the pipeline enforces, including baseline declarations.
            var resolution = runtime.Resolve(declaration.Requirements, scope.ServiceProvider, CancellationToken.None).GetAwaiter().GetResult();
            return resolution is IAnonymousPolicyResolution { EvaluatesAnonymous: true };
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            // An invalid declaration never gains anonymous metadata. Leave the existing startup
            // validator responsible for reporting the configuration error before accepting traffic.
            return false;
        }
    }
}
