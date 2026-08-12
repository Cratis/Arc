// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_FixedTenantIdResolver.given;

public class a_fixed_tenant_id_resolver : Specification
{
    protected FixedTenantIdResolver _resolver;
    protected IOptions<ArcOptions> _options;

    void Establish()
    {
        _options = Substitute.For<IOptions<ArcOptions>>();

        var arcOptions = new ArcOptions();
        _options.Value.Returns(arcOptions);

        _resolver = new FixedTenantIdResolver(_options);
    }
}
