// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for <see cref="DbContextOptionsBuilder"/> to add observation support.
/// </summary>
public static class ObserveDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds observation interceptor to the DbContext options.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve services from.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder AddObservation(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var changeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
        var logger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
        optionsBuilder.AddInterceptors(new ObserveInterceptor(changeTracker, logger));
        return optionsBuilder;
    }
}
