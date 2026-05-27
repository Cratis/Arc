// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
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
[Singleton]
public class IntrospectionService : IIntrospectionService
{
    static readonly Dictionary<string, XDocument?> _xmlDocCache = [];
    static readonly char[] _lineSeparators = ['\n', '\r'];

    readonly Lazy<IReadOnlyList<CommandIntrospectionMetadata>> _commands;
    readonly Lazy<IReadOnlyList<QueryIntrospectionMetadata>> _queries;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectionService"/> class.
    /// </summary>
    /// <param name="commandHandlerProviders">The provider of all discovered command handlers.</param>
    /// <param name="queryPerformerProviders">The provider of all discovered query performers.</param>
    /// <param name="options">Configuration options for routing.</param>
    /// <param name="arcOptions">General Arc options, including JSON serialization settings.</param>
    public IntrospectionService(
        ICommandHandlerProviders commandHandlerProviders,
        IQueryPerformerProviders queryPerformerProviders,
        IOptions<ApiEndpointOptions> options,
        IOptions<ArcOptions> arcOptions)
    {
        var schemaGenerationOptions = CreateSchemaGenerationOptions(arcOptions.Value.JsonSerializerOptions);
        _commands = new(() => BuildCommands(commandHandlerProviders, options.Value, schemaGenerationOptions));
        _queries = new(() => BuildQueries(queryPerformerProviders, options.Value, schemaGenerationOptions));
    }

    /// <inheritdoc/>
    public IReadOnlyList<CommandIntrospectionMetadata> Commands => _commands.Value;

    /// <inheritdoc/>
    public IReadOnlyList<QueryIntrospectionMetadata> Queries => _queries.Value;

    static List<CommandIntrospectionMetadata> BuildCommands(ICommandHandlerProviders providers, ApiEndpointOptions options, JsonSerializerOptions schemaGenerationOptions)
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
            var route = EndpointRouteHelper.BuildRouteUrl(options, handler.Location, options.SegmentsToSkipForRoute, handler.CommandType.Name, includeCommandName);

            return new CommandIntrospectionMetadata(
                handler.CommandType.Name,
                string.Join('.', location),
                route,
                handler.CommandType.FullName ?? handler.CommandType.Name,
                GetDocumentationSummary(handler.CommandType),
                GetTypeSchema(handler.CommandType, schemaGenerationOptions));
        }).ToList();
    }

    static List<QueryIntrospectionMetadata> BuildQueries(IQueryPerformerProviders providers, ApiEndpointOptions options, JsonSerializerOptions schemaGenerationOptions)
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
            var route = EndpointRouteHelper.BuildRouteUrl(options, performer.Location, options.SegmentsToSkipForRoute, performer.Name.ToString(), includeQueryName);

            return new QueryIntrospectionMetadata(
                performer.Name.ToString(),
                string.Join('.', location),
                route,
                performer.FullyQualifiedName.ToString(),
                performer.Type.FullName ?? performer.Type.Name,
                GetDocumentationSummary(performer.Type),
                GetArgumentsSchema(performer.Parameters, schemaGenerationOptions));
        }).ToList();
    }

    static JsonSerializerOptions CreateSchemaGenerationOptions(JsonSerializerOptions baseOptions)
    {
        var schemaGenerationOptions = new JsonSerializerOptions(baseOptions);
        schemaGenerationOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        return schemaGenerationOptions;
    }

    static JsonNode GetTypeSchema(Type type, JsonSerializerOptions schemaGenerationOptions) => schemaGenerationOptions.GetJsonSchemaAsNode(type);

    static JsonObject GetArgumentsSchema(QueryParameters parameters, JsonSerializerOptions schemaGenerationOptions)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var parameter in parameters)
        {
            properties[parameter.Name] = GetTypeSchema(parameter.Type, schemaGenerationOptions);
            if (parameter.IsRequired)
            {
                required.Add(parameter.Name);
            }
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
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
