// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Reads declared routing without inventing source or stream classifications.</summary>
/// <param name="models">The source models.</param>
/// <param name="types">The type registry.</param>
/// <param name="diagnostics">The diagnostic sink.</param>
/// <param name="enabled">Whether authoring routes are enabled.</param>
public class CommandRouteReader(SemanticModels models, TypeRegistry types, ScreenplayDiagnostics diagnostics, bool enabled)
{
    /// <summary>Reads a command's source-owned stream and property-backed stream id.</summary>
    /// <param name="command">The command.</param>
    /// <param name="identifier">The command identity property.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns>The route, or null when it cannot be stated.</returns>
    public CommandRouteModel? Read(INamedTypeSymbol command, string? identifier, string location)
    {
        var definition = EventSourceReader.Read(command);
        var source = definition?.Source ?? command.GetAttribute(WellKnownTypeNames.EventSourceTypeAttribute)?.GetArgument(0) as string;
        var stream = definition?.Stream ?? command.GetAttribute(WellKnownTypeNames.EventStreamTypeAttribute)?.GetArgument(0) as string;
        if (source is null && stream is null)
        {
            return null;
        }

        if (!enabled)
        {
            return null;
        }

        if (source is null || !ScreenplayIdentifier.IsBareIdentifier(source) || (stream is not null && !ScreenplayIdentifier.IsBareIdentifier(stream)) || definition is { StreamDeclared: false })
        {
            Report("The route has no uniquely declared source and stream with grammar-valid names; no route was inferred", location);
            return null;
        }

        var identity = command.DeclaredProperties().FirstOrDefault(property => property.Name == identifier);
        if (stream is null)
        {
            Report("The source declaration is readable, but command routing without a selected stream has no grammar counterpart", location);
            return new(source, null, identity is null ? null : types.Resolve(identity.Type), null, null);
        }

        string? streamId = null;
        TypeReferenceModel? streamIdType = null;
        if (command.FindInterface("Cratis.Chronicle.Events.ICanProvideEventStreamId") is { } provider)
        {
            var contract = provider.GetMembers("GetEventStreamId").OfType<IMethodSymbol>().SingleOrDefault();
            var implementation = contract is null ? null : command.FindImplementationForInterfaceMember(contract) as IMethodSymbol;
            var body = implementation is null ? null : HandlerBodies.Of(implementation).SingleOrDefault();
            var expression = body is BlockSyntax { Statements: [ReturnStatementSyntax returned] } ? returned.Expression : body as ExpressionSyntax;
            var model = expression is null ? null : models.For(expression.SyntaxTree);
            var path = expression is null || model is null ? null : MappingSourceReader.ReadPath(expression, model, command);
            var property = command.DeclaredProperties().FirstOrDefault(property => property.Name == path);
            if (property is null || !PortableStreamId(property.Type))
            {
                Report("GetEventStreamId() is not a directly returned portable command property; its stream id mapping was left in code", location);
                return null;
            }

            streamId = property.Name;
            streamIdType = types.Resolve(property.Type);
        }
        else if (command.GetAttribute(WellKnownTypeNames.EventStreamIdAttribute)?.GetArgument(0) is string)
        {
            Report("A literal stream id attribute is not a property-backed route; its stream id was left in code", location);
            return null;
        }

        return new(source, stream, identity is null ? null : types.Resolve(identity.Type), streamIdType, streamId);
    }

    static bool PortableStreamId(ITypeSymbol type)
    {
        var backing = type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments.FirstOrDefault();
        var scalar = backing ?? type;
        return type.NullableAnnotation != NullableAnnotation.Annotated &&
            (scalar.SpecialType == SpecialType.System_String || scalar.Is("System.Guid") ||
                (backing is not null && scalar.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64));
    }

    void Report(string message, string location) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandRoute, message, location);
}
