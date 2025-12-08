// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/> for adding the correlation Id log enricher.
/// </summary>
public static class CorrelationIdHostBuilderExtensions
{
    /// <summary>
    /// Adds the correlation ID enricher to the logging configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder AddCorrelationIdLogEnricher(this IHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.EnableEnrichment());
        builder.ConfigureServices(services => services.AddLogEnricher<CorrelationIdLogEnricher>());
        return builder;
    }
}