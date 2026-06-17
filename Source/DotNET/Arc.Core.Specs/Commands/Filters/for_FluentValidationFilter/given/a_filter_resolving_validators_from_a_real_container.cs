// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Arc.Validation.for_DiscoverableValidators;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.given;

/// <summary>
/// Reproduces the runtime/HTTP condition: a container with scope validation enabled (as ASP.NET Core enables in
/// development) where a validator depends on a scoped collaborator — the stand-in for a read model resolved for the
/// command's event source id. The command runs in a child scope, exactly as the HTTP endpoint runs it through
/// <c>RequestServices</c>.
/// </summary>
public class a_filter_resolving_validators_from_a_real_container : Specification
{
    protected FluentValidationFilter _filter;
    protected ServiceProvider _root;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _filter = new FluentValidationFilter(new DiscoverableValidators(Cratis.Types.Types.Instance));
        _root = new ServiceCollection()
            .AddScoped(_ => new CommandDependency { IsAllowed = false })
            .AddTransient<CommandWithDependencyValidator>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Destroy() => _root.Dispose();
}
