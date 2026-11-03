// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider.given;

public class a_provider_with_an_assembly_filter : a_query_performer_provider_with_no_metadata
{
    protected IServiceScopeFactory _scopeFactory;
    private protected ArtifactAssemblyFilter _excluded;
    private protected ArtifactAssemblyFilter _included;
    protected string _queryName;

    void Establish()
    {
        _types.All.Returns([typeof(PublicReadModelWithValidQuery)]);
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _excluded = new ArtifactAssemblyFilter([], _ => true);
        _included = new ArtifactAssemblyFilter([typeof(PublicReadModelWithValidQuery).Assembly], _ => true);
        _queryName = $"{typeof(PublicReadModelWithValidQuery).FullName}.{nameof(PublicReadModelWithValidQuery.GetById)}";
    }

    private protected QueryPerformerProvider CreateProvider(ArtifactAssemblyFilter filter) =>
        new(
            _types,
            _registry,
            QueryPerformerProvider.CreateScopedPerformerFactory(_serviceProviderIsService, _scopeFactory, sp => sp.GetRequiredService<IAuthorizationEvaluator>()),
            filter);
}
