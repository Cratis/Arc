// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static class ProtectedKeyTraversalForSpecs
{
    internal static ProtectedReadModelKeyTraversal Create(ProtectedReadModelKeyOptions? options = null) =>
        new(Options.Create(options ?? new()), NullLogger<ProtectedReadModelKeyTraversal>.Instance);

    internal static IServiceCollection AddProtectedKeyTraversal(this IServiceCollection services, ProtectedReadModelKeyOptions? options = null) =>
        services.AddSingleton(Create(options));
}
