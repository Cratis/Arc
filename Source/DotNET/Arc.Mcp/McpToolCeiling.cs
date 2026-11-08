// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
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
    /// <returns>The frozen ceiling.</returns>
    /// <exception cref="McpToolNameInvalid">A name is not a stable snake_case name.</exception>
    /// <exception cref="McpToolNameDuplicated">A name is declared twice.</exception>
    /// <exception cref="McpToolDescriptionMissing">A description is missing.</exception>
    /// <exception cref="McpExposedOperationUnknown">An operation is unknown to Arc.</exception>
    /// <exception cref="McpApprovalRequiredCommandExposed">A command requiring approval is exposed.</exception>
    /// <exception cref="McpToolHasNoScope">A tool would require no scope.</exception>
    internal static McpToolCeiling Build(
        McpExposure exposure,
        McpOptions options,
        ICommandHandlerProviders commands,
        IQueryPerformerProviders queries)
    {
        var handlerTypes = commands.Handlers.Select(_ => _.CommandType).ToHashSet();
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

            tools.Add(declaration.Name, Describe(declaration, options, handlerTypes, performers));
        }

        return new McpToolCeiling(tools.Values);
    }

    static McpToolDescriptor Describe(
        McpExposureDeclaration declaration,
        McpOptions options,
        HashSet<Type> handlerTypes,
        IQueryPerformer[] performers)
    {
        var isCommand = declaration.Kind == McpToolKind.Command;
        if (isCommand)
        {
            var commandType = declaration.Operation as Type ?? throw new McpExposedOperationUnknown(declaration.Name, declaration.Operation);
            if (commandType.IsDefined(typeof(Identity.Approvals.RequiresApprovalAttribute), true))
            {
                throw new McpApprovalRequiredCommandExposed(declaration.Name, commandType);
            }
            if (!handlerTypes.Contains(commandType))
            {
                throw new McpExposedOperationUnknown(declaration.Name, commandType);
            }
        }
        else if (declaration.Kind != McpToolKind.Query ||
            declaration.Operation is not string query ||
            !performers.Any(_ => string.Equals(_.FullyQualifiedName.Value, query, StringComparison.Ordinal)))
        {
            throw new McpExposedOperationUnknown(declaration.Name, declaration.Operation);
        }

        var scope = isCommand ? options.WriteScope : options.ReadScope;
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new McpToolHasNoScope(declaration.Name);
        }

        return new McpToolDescriptor(
            declaration.Name,
            declaration.Description!,
            declaration.Kind,
            isCommand ? declaration.Operation : new FullyQualifiedQueryName((string)declaration.Operation),
            Array.AsReadOnly([scope]),
            isCommand ? McpToolAccess.Write : McpToolAccess.Read,
            null,
            isCommand ? McpToolAnnotations.ForWrite : McpToolAnnotations.ForRead);
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

    static bool IsValidName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength || name[0] is < 'a' or > 'z')
        {
            return false;
        }

        return name.All(_ => _ is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');
    }
}
