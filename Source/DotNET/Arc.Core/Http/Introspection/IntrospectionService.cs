// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.Introspection;

/// <summary>
/// Represents an implementation of <see cref="IIntrospectionService"/> that uses reflection to discover command handlers and query performers, mapping their endpoints and extracting XML documentation summaries.
/// </summary>
/// <param name="serviceProvider">The service provider for accessing service implementations.</param>
/// <param name="options">Configuration options for routing.</param>
public class IntrospectionService(IServiceProvider serviceProvider, IOptions<ApiEndpointOptions> options) : IIntrospectionService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ApiEndpointOptions _options = options.Value;

    /// <summary>
    /// Introspects all registered commands and queries, mapping their endpoints and retrieving associated documentation.
    /// </summary>
    /// <returns>A tuple containing lists of discovered commands and queries metadata.</returns>
    public (List<CommandIntrospectionMetadata> Commands, List<QueryIntrospectionMetadata> Queries) IntrospectAllEndpoints()
    {
        var commandHandlers = _serviceProvider.GetRequiredService<ICommandHandlerProviders>().Handlers;
        var discoveredCommands = new List<CommandIntrospectionMetadata>();

        foreach (var handler in commandHandlers)
        {
            var location = handler.Location.Skip(_options.SegmentsToSkipForRoute);
            var routePath = EndpointRouteHelper.BuildRouteUrl(_options, location, handler.CommandType.Name, true);

            var commandMeta = new CommandIntrospectionMetadata(
                handler.CommandType.Name,
                string.Join('.', location),
                routePath,
                handler.CommandType.FullName ?? handler.CommandType.Name,
                GetDocumentationSummary(handler.CommandType));

            discoveredCommands.Add(commandMeta);
        }

        var queryPerformers = _serviceProvider.GetRequiredService<IQueryPerformerProviders>().Performers;
        var discoveredQueries = new List<QueryIntrospectionMetadata>();

        foreach (var performer in queryPerformers)
        {
            var location = performer.Location.Skip(_options.SegmentsToSkipForRoute);
            var routePath = EndpointRouteHelper.BuildRouteUrl(_options, location, performer.Name, true);

            var queryMeta = new QueryIntrospectionMetadata(
                performer.Name,
                string.Join('.', location),
                routePath,
                performer.FullyQualifiedName,
                GetDocumentationSummary(performer.Type));

            discoveredQueries.Add(queryMeta);
        }

        return (discoveredCommands, discoveredQueries);
    }

    static string GetDocumentationSummary(Type type)
    {
        var documentation = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        if (string.IsNullOrWhiteSpace(documentation))
        {
            return string.Empty;
        }

        return documentation;
    }
}