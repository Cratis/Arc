// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_generation_uses_the_default_tenant : given.a_context_generated_value_resolver
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_map_both_default_tenant_sentinels(bool named)
    {
        _tenant.Current.Returns(named ? TenantId.Default : TenantId.NotSet);
        var resolution = await Resolve();
        ((ContextId)resolution.Arguments.Single()!).Value.ShouldEqual($"{_now:O}|{GenerationContext.DefaultTenant}");
    }
}
