// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.for_IdentityProviderServiceCollectionExtensions.given;

public class a_service_collection_with_options : Specification
{
    protected IServiceCollection _services;
    protected ITypes _types;

    void Establish()
    {
        _services = new ServiceCollection();
        _types = Substitute.For<ITypes>();

        _services.AddSingleton(_types);
        _services.AddOptions<ArcOptions>();
    }
}
