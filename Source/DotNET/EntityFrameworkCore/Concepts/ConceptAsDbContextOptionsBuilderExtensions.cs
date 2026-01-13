// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Extension methods for adding ConceptAs support to DbContextOptionsBuilder.
/// </summary>
public static class ConceptAsDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds support for ConceptAs types in LINQ queries. Call this method when configuring your DbContext
    /// to enable automatic handling of ConceptAs types in Where, Select, and other LINQ operations.
    /// </summary>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    /// <item><description>ConceptAsQueryExpressionInterceptor: Rewrites LINQ expressions to handle ConceptAs types</description></item>
    /// <item><description>ConceptAsDbCommandInterceptor: Unwraps ConceptAs parameter values before SQL execution</description></item>
    /// <item><description>ConceptAsEvaluatableExpressionFilter: Controls how ConceptAs expressions are evaluated</description></item>
    /// <item><description>ConceptAsModelCustomizer: Configures the model to handle ConceptAs properties</description></item>
    /// </list>
    /// </remarks>
    public static DbContextOptionsBuilder AddConceptAsSupport(this DbContextOptionsBuilder optionsBuilder)
    {
        // Add interceptors for query expression rewriting and parameter unwrapping
        optionsBuilder.AddInterceptors(
            new ConceptAsQueryExpressionInterceptor(),
            new ConceptAsDbCommandInterceptor());

        // Replace services to handle ConceptAs types in query evaluation and model customization
        // The service replacement can cause multiple service providers to be created in test scenarios
        // which is expected behavior and doesn't affect production usage with pooled context factories
        optionsBuilder
            .ReplaceService<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
            .ReplaceService<IModelCustomizer, ConceptAsModelCustomizer>()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        return optionsBuilder;
    }

    /// <summary>
    /// Adds support for ConceptAs types in LINQ queries. Call this method when configuring your DbContext
    /// to enable automatic handling of ConceptAs types in Where, Select, and other LINQ operations.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext being configured.</typeparam>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    public static DbContextOptionsBuilder<TContext> AddConceptAsSupport<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)optionsBuilder).AddConceptAsSupport();
        return optionsBuilder;
    }
}
