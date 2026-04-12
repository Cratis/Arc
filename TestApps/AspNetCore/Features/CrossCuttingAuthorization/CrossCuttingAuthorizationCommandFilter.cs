// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Http;

namespace AspNetCore.Features.CrossCuttingAuthorization;

/// <summary>
/// Represents a command filter that applies cross-cutting authorization to a feature namespace.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> for resolving the current user principal.</param>
public class CrossCuttingAuthorizationCommandFilter(IHttpRequestContextAccessor httpRequestContextAccessor) : ICommandFilter
{
    const string ProtectedNamespace = "TestApps.Features.CrossCuttingAuthorization";
    const string RequiredRole = "CrossCuttingAuthorization";

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        if (!IsProtectedCommand(context.Type))
        {
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }

        if (httpRequestContextAccessor.Current?.User.IsInRole(RequiredRole) ?? false)
        {
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }

        return Task.FromResult(CommandResult.Unauthorized(context.CorrelationId, $"Role '{RequiredRole}' is required for commands in namespace '{ProtectedNamespace}'."));
    }

    static bool IsProtectedCommand(Type commandType) =>
        commandType.Namespace?.StartsWith(ProtectedNamespace, StringComparison.Ordinal) ?? false;
}
