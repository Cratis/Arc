// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Maps an explicit exposure declaration to the descriptor of a tool.
/// </summary>
/// <remarks>
/// The mapper is the one place that decides what a tool needs: the scope of its operation kind merged with every
/// <see cref="RequiresScopeAttribute"/> the operation declares, the annotations of its kind, its input schema, and the
/// refusal of commands that require approval, including ones that inherit the requirement.
/// </remarks>
static class McpToolMapper
{
    /// <summary>
    /// Maps a declaration to a tool descriptor.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="options">The MCP options.</param>
    /// <param name="handlers">The known command handlers.</param>
    /// <param name="performers">The known query performers.</param>
    /// <param name="serializerOptions">The effective Arc serializer options.</param>
    /// <returns>The descriptor.</returns>
    /// <exception cref="McpExposedOperationUnknown">The operation is unknown to Arc.</exception>
    /// <exception cref="McpApprovalRequiredCommandExposed">The command requires approval, directly or by inheritance.</exception>
    /// <exception cref="McpToolHasNoScope">The tool would require no scope.</exception>
    /// <exception cref="McpToolScopeBlank">The operation declares a blank scope.</exception>
    internal static McpToolDescriptor Map(
        McpExposureDeclaration declaration,
        McpOptions options,
        IReadOnlyCollection<ICommandHandler> handlers,
        IReadOnlyCollection<IQueryPerformer> performers,
        JsonSerializerOptions? serializerOptions = null)
    {
        serializerOptions ??= new JsonSerializerOptions().ConfigureArcDefaults();
        return declaration.Kind switch
        {
            McpToolKind.Command => MapCommand(declaration, options, handlers, serializerOptions),
            McpToolKind.Query => MapQuery(declaration, options, performers, serializerOptions),
            _ => throw new McpExposedOperationUnknown(declaration.Name, declaration.Operation)
        };
    }

    static McpToolDescriptor MapCommand(McpExposureDeclaration declaration, McpOptions options, IReadOnlyCollection<ICommandHandler> handlers, JsonSerializerOptions serializerOptions)
    {
        var commandType = declaration.Operation as Type ?? throw new McpExposedOperationUnknown(declaration.Name, declaration.Operation);
        if (commandType.IsDefined(typeof(RequiresApprovalAttribute), true))
        {
            throw new McpApprovalRequiredCommandExposed(declaration.Name, commandType);
        }

        var matching = handlers.Where(_ => _.CommandType == commandType).ToArray();
        if (matching.Length == 0)
        {
            throw new McpExposedOperationUnknown(declaration.Name, commandType);
        }

        var carriers = new List<MemberInfo> { commandType };
        carriers.AddRange(commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(_ => _.Name == "Handle"));
        carriers.AddRange(matching.SelectMany(_ => _.Parameters).Select(_ => _.Member));

        return new McpToolDescriptor(
            declaration.Name,
            declaration.Description!,
            McpToolKind.Command,
            commandType,
            ScopesOf(declaration.Name, options.WriteScope, carriers),
            McpToolAccess.Write,
            McpSchemaBuilder.ForCommand(commandType, serializerOptions),
            McpToolAnnotations.ForWrite);
    }

    static McpToolDescriptor MapQuery(McpExposureDeclaration declaration, McpOptions options, IReadOnlyCollection<IQueryPerformer> performers, JsonSerializerOptions serializerOptions)
    {
        var performer = (declaration.Operation is string query
            ? performers.FirstOrDefault(_ => string.Equals(_.FullyQualifiedName.Value, query, StringComparison.Ordinal))
            : null) ?? throw new McpExposedOperationUnknown(declaration.Name, declaration.Operation);

        var carriers = new List<MemberInfo>();
        foreach (var type in new[] { performer.ReadModelType, performer.Type }.Where(_ => _ is not null).Distinct())
        {
            carriers.Add(type);
            carriers.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(_ => _.Name == performer.Name.Value));
        }

        return new McpToolDescriptor(
            declaration.Name,
            declaration.Description!,
            McpToolKind.Query,
            performer.FullyQualifiedName,
            ScopesOf(declaration.Name, options.ReadScope, carriers),
            McpToolAccess.Read,
            McpSchemaBuilder.ForQuery(performer.Parameters, performer.SupportsPaging, serializerOptions, McpArgumentDescriptions.ForQuery(performer)),
            McpToolAnnotations.ForRead);
    }

    /// <summary>
    /// Merges the scope of the operation kind with every declared scope. The kind's scope comes first; the rest follow in
    /// ordinal order, without duplicates, so the result is stable.
    /// </summary>
    /// <param name="toolName">The tool name, for failures.</param>
    /// <param name="kindScope">The scope of the operation kind.</param>
    /// <param name="carriers">The types and methods that may declare scopes.</param>
    /// <returns>The merged scopes.</returns>
    /// <exception cref="McpToolHasNoScope">The kind scope is not configured.</exception>
    /// <exception cref="McpToolScopeBlank">A declared scope is blank.</exception>
    static ReadOnlyCollection<string> ScopesOf(string toolName, string? kindScope, IEnumerable<MemberInfo> carriers)
    {
        if (string.IsNullOrWhiteSpace(kindScope))
        {
            throw new McpToolHasNoScope(toolName);
        }

        var declared = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var attribute in carriers.Distinct().SelectMany(_ => _.GetCustomAttributes<RequiresScopeAttribute>(true)))
        {
            if (string.IsNullOrWhiteSpace(attribute.Scope))
            {
                throw new McpToolScopeBlank(toolName);
            }

            declared.Add(attribute.Scope);
        }

        declared.Remove(kindScope);
        return Array.AsReadOnly([kindScope, .. declared]);
    }
}
