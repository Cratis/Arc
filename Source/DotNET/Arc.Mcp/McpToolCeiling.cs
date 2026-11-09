// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the frozen set of tools that can ever be reached over MCP. Anything not in it is unreachable.
/// </summary>
public sealed class McpToolCeiling
{
    const int MaxNameLength = 64;

    readonly FrozenDictionary<string, McpToolDescriptor> _tools;

    McpToolCeiling(IEnumerable<McpToolDescriptor> tools)
    {
        _tools = tools.ToFrozenDictionary(_ => _.Name, StringComparer.Ordinal);
        Tools = _tools.Values;
    }

    /// <summary>
    /// Gets a value indicating whether the ceiling holds no tools.
    /// </summary>
    public bool IsEmpty => _tools.Count == 0;

    /// <summary>
    /// Gets the tools in the ceiling.
    /// </summary>
    public IReadOnlyCollection<McpToolDescriptor> Tools { get; }

    /// <summary>
    /// Looks up a tool by its exact, case-sensitive name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="tool">The tool, when found.</param>
    /// <returns>True if the ceiling holds the tool; otherwise false.</returns>
    public bool TryGet(string? name, out McpToolDescriptor? tool)
    {
        tool = null;
        return name is not null && _tools.TryGetValue(name, out tool);
    }

    /// <summary>
    /// Builds and validates the ceiling from the explicit exposure and attribute declarations.
    /// </summary>
    /// <param name="exposure">The explicit exposure.</param>
    /// <param name="options">The MCP options.</param>
    /// <param name="commands">The command handler providers.</param>
    /// <param name="queries">The query performer providers.</param>
    /// <param name="sources">The registered tool sources contributing curated tools.</param>
    /// <param name="serializerOptions">The effective Arc serializer options.</param>
    /// <returns>The frozen ceiling.</returns>
    /// <exception cref="McpToolNameInvalid">A name is not a stable snake_case name.</exception>
    /// <exception cref="McpToolNameDuplicated">A name is declared twice.</exception>
    /// <exception cref="McpToolDescriptionMissing">A description is missing.</exception>
    /// <exception cref="McpExposedOperationUnknown">An operation is unknown to Arc.</exception>
    /// <exception cref="McpApprovalRequiredCommandExposed">A command requiring approval is exposed.</exception>
    /// <exception cref="McpToolHasNoScope">A tool would require no scope.</exception>
    /// <exception cref="McpToolScopeBlank">An exposed operation declares a blank required scope.</exception>
    /// <exception cref="McpOptionsInvalid">A write scope is blank, repeated or equal to the read scope.</exception>
    /// <exception cref="McpWriteToolLacksWriteScope">A curated tool that may change state does not require a write scope in every scope set.</exception>
    /// <exception cref="McpToolOutputSchemaInvalid">A curated tool declares an output schema that does not describe an object.</exception>
    internal static McpToolCeiling Build(
        McpExposure exposure,
        McpOptions options,
        ICommandHandlerProviders commands,
        IQueryPerformerProviders queries,
        IEnumerable<IMcpToolSource>? sources = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        serializerOptions ??= new JsonSerializerOptions().ConfigureArcDefaults();
        var writeScopes = WriteScopesOf(options);
        var handlers = commands.Handlers.ToArray();
        var handlerTypes = handlers.Select(_ => _.CommandType).ToHashSet();
        var performers = queries.Performers.ToArray();
        var tools = new Dictionary<string, McpToolDescriptor>(StringComparer.Ordinal);

        foreach (var declaration in exposure.Declarations.Concat(Discover(handlerTypes, performers)))
        {
            if (!IsValidName(declaration.Name))
            {
                throw new McpToolNameInvalid(declaration.Name);
            }
            if (tools.ContainsKey(declaration.Name))
            {
                throw new McpToolNameDuplicated(declaration.Name);
            }
            if (string.IsNullOrWhiteSpace(declaration.Description))
            {
                throw new McpToolDescriptionMissing(declaration.Name);
            }

            tools.Add(declaration.Name, McpToolMapper.Map(declaration, options, handlers, performers, serializerOptions));
        }

        foreach (var source in sources ?? [])
        {
            foreach (var described in source.Describe())
            {
                var tool = Curated(described, source, writeScopes);
                if (!tools.TryAdd(tool.Name, tool))
                {
                    throw new McpToolNameDuplicated(tool.Name);
                }
            }
        }

        return new McpToolCeiling(tools.Values);
    }

    static IEnumerable<McpExposureDeclaration> Discover(HashSet<Type> handlerTypes, IQueryPerformer[] performers)
    {
        foreach (var type in handlerTypes)
        {
            if (type.GetCustomAttribute<ExposeOverMcpAttribute>(false) is { } attribute)
            {
                yield return new McpExposureDeclaration(McpToolKind.Command, type, attribute.Name, attribute.Description);
            }
        }

        foreach (var performer in performers)
        {
            var methods = performer.ReadModelType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(_ => _.Name == performer.Name.Value);
            foreach (var attribute in methods.Select(_ => _.GetCustomAttribute<ExposeOverMcpAttribute>(false)).OfType<ExposeOverMcpAttribute>())
            {
                yield return new McpExposureDeclaration(McpToolKind.Query, performer.FullyQualifiedName.Value, attribute.Name, attribute.Description);
            }
        }
    }

    static FrozenSet<string> WriteScopesOf(McpOptions options)
    {
        var listed = options.WriteScopes;
        if (listed.Any(string.IsNullOrWhiteSpace) ||
            listed.Distinct(StringComparer.Ordinal).Count() != listed.Count)
        {
            throw new McpOptionsInvalid(nameof(McpOptions.WriteScopes), options.Profile);
        }

        var writeScopes = options.ConfiguredWriteScopes().ToFrozenSet(StringComparer.Ordinal);
        if (options.ReadScope is { } readScope && writeScopes.Contains(readScope))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.WriteScopes), options.Profile);
        }

        return writeScopes;
    }

    static McpToolDescriptor Curated(McpToolDescriptor described, IMcpToolSource source, FrozenSet<string> writeScopes)
    {
        if (!IsValidName(described.Name))
        {
            throw new McpToolNameInvalid(described.Name);
        }
        if (string.IsNullOrWhiteSpace(described.Description))
        {
            throw new McpToolDescriptionMissing(described.Name);
        }
        if (described.RequiredScopes is not { Count: > 0 } scopes)
        {
            throw new McpToolHasNoScope(described.Name);
        }
        if (scopes.Any(string.IsNullOrWhiteSpace))
        {
            throw new McpToolScopeBlank(described.Name);
        }
        var alternatives = described.AlternativeScopes ?? [];
        if (alternatives.Any(_ => _ is not { Count: > 0 }))
        {
            throw new McpToolHasNoScope(described.Name);
        }
        if (alternatives.Any(_ => _.Any(string.IsNullOrWhiteSpace)))
        {
            throw new McpToolScopeBlank(described.Name);
        }
        if (described.Access == McpToolAccess.Write &&
            !alternatives.Prepend(scopes).All(_ => _.Any(writeScopes.Contains)))
        {
            throw new McpWriteToolLacksWriteScope(described.Name);
        }
        if (described.OutputSchema is not null && !DescribesAnObject(described.OutputSchema))
        {
            throw new McpToolOutputSchemaInvalid(described.Name);
        }

        return described with
        {
            Kind = McpToolKind.Custom,
            Operation = source,
            RequiredScopes = [.. scopes],
            AlternativeScopes = [.. alternatives.Select(_ => (IReadOnlyList<string>)[.. _])],
            InputSchema = described.InputSchema ?? """{"type":"object"}"""
        };
    }

    static bool DescribesAnObject(string schema)
    {
        try
        {
            using var document = JsonDocument.Parse(schema);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String &&
                type.GetString() == "object";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool IsValidName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength || name[0] is < 'a' or > 'z')
        {
            return false;
        }

        return name.All(_ => _ is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');
    }
}
