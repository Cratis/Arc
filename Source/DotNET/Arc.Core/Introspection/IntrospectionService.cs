// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Represents an implementation of <see cref="IIntrospectionService"/> that discovers command and query endpoints
/// using the same route-building logic as the endpoint mappers, and extracts XML documentation summaries.
/// </summary>
/// <param name="commandHandlerProviders">The provider of all discovered command handlers.</param>
/// <param name="queryPerformerProviders">The provider of all discovered query performers.</param>
/// <param name="options">Configuration options for routing.</param>
[Singleton]
public class IntrospectionService(
    ICommandHandlerProviders commandHandlerProviders,
    IQueryPerformerProviders queryPerformerProviders,
    IOptions<ApiEndpointOptions> options) : IIntrospectionService
{
    static readonly Dictionary<string, XDocument?> _xmlDocCache = [];
    static readonly char[] _lineSeparators = ['\n', '\r'];

    readonly Lazy<IReadOnlyList<CommandIntrospectionMetadata>> _commands = new(() => BuildCommands(commandHandlerProviders, options.Value));
    readonly Lazy<IReadOnlyList<QueryIntrospectionMetadata>> _queries = new(() => BuildQueries(queryPerformerProviders, options.Value));

    /// <inheritdoc/>
    public IReadOnlyList<CommandIntrospectionMetadata> Commands => _commands.Value;

    /// <inheritdoc/>
    public IReadOnlyList<QueryIntrospectionMetadata> Queries => _queries.Value;

    static List<CommandIntrospectionMetadata> BuildCommands(ICommandHandlerProviders providers, ApiEndpointOptions options)
    {
        var handlersByNamespace = EndpointRouteHelper.GroupByNamespace(
            providers.Handlers,
            h => h.Location,
            options.SegmentsToSkipForRoute);

        return providers.Handlers.Select(handler =>
        {
            var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
            var includeCommandName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeCommandNameInRoute,
                location,
                handlersByNamespace);
            var route = EndpointRouteHelper.BuildRouteUrl(options, location, handler.CommandType.Name, includeCommandName);

            return new CommandIntrospectionMetadata(
                handler.CommandType.Name,
                string.Join('.', location),
                route,
                handler.CommandType.FullName ?? handler.CommandType.Name,
                GetDocumentationSummary(handler.CommandType));
        }).ToList();
    }

    static List<QueryIntrospectionMetadata> BuildQueries(IQueryPerformerProviders providers, ApiEndpointOptions options)
    {
        var performersByNamespace = EndpointRouteHelper.GroupByNamespace(
            providers.Performers,
            p => p.Location,
            options.SegmentsToSkipForRoute);

        return providers.Performers.Select(performer =>
        {
            var location = performer.Location.Skip(options.SegmentsToSkipForRoute);
            var includeQueryName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeQueryNameInRoute,
                location,
                performersByNamespace);
            var route = EndpointRouteHelper.BuildRouteUrl(options, location, performer.Name.ToString(), includeQueryName);

            return new QueryIntrospectionMetadata(
                performer.Name.ToString(),
                string.Join('.', location),
                route,
                performer.FullyQualifiedName.ToString(),
                performer.Type.FullName ?? performer.Type.Name,
                GetDocumentationSummary(performer.Type));
        }).ToList();
    }

    static string GetDocumentationSummary(Type type)
    {
        var xmlFile = Path.ChangeExtension(type.Assembly.Location, ".xml");

        if (!_xmlDocCache.TryGetValue(xmlFile, out var xmlDoc))
        {
            xmlDoc = File.Exists(xmlFile) ? XDocument.Load(xmlFile) : null;
            _xmlDocCache[xmlFile] = xmlDoc;
        }

        if (xmlDoc is null)
        {
            return string.Empty;
        }

        var memberName = $"T:{type.FullName}";
        var member = xmlDoc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (member is null)
        {
            return string.Empty;
        }

        var summary = member.Element("summary");
        if (summary is null)
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            summary.Value
                .Split(_lineSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s)));
    }
}
