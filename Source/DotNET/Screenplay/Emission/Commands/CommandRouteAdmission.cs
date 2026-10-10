// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Classifies a complete command route before any of its declarations are emitted.
/// </summary>
internal static class CommandRouteAdmission
{
    /// <summary>
    /// Compares the declared key shapes of two streams without comparing their command mappings.
    /// </summary>
    /// <param name="left">The first declaration.</param>
    /// <param name="right">The second declaration.</param>
    /// <returns>Whether both routes declare the same stream key shape.</returns>
    internal static bool SameStream(CommandRouteModel left, CommandRouteModel right) => left.StreamIdType == right.StreamIdType &&
        left.StreamIdParts.Select(part => (part.Name, part.Type)).SequenceEqual(right.StreamIdParts.Select(part => (part.Name, part.Type)));

    /// <summary>
    /// Gets the reason a route cannot be emitted completely in the selected mode.
    /// </summary>
    /// <param name="route">The entire route.</param>
    /// <param name="command">The command supplying its mappings.</param>
    /// <param name="model">The concepts typing the route.</param>
    /// <param name="authoring">Whether syntax-only mappings are allowed.</param>
    /// <returns>The refusal reason, or null for an admitted route.</returns>
    internal static string? Failure(CommandRouteModel route, CommandModel command, ApplicationModel model, bool authoring)
    {
        if (route.Stream is null)
        {
            return authoring ? null : "routing without a selected stream has no grammar counterpart";
        }
        if (!authoring && !command.Produces.Any())
        {
            return "routes on handler commands are not admitted (PLAY0268)";
        }
        if (route.StreamIdParts.Count > 0)
        {
            if (route.StreamIdType is not null || route.StreamId is not null || route.StreamIdLiteral is not null || route.StreamIdParts.Count < 2 ||
                route.StreamIdParts.Select(part => new ScreenplayNaming().ToPropertyName(part.Name)).Distinct(StringComparer.Ordinal).Count() != route.StreamIdParts.Count)
            {
                return "a composite stream id requires at least two distinct parts and cannot also carry a scalar mapping (PLAY0273)";
            }

            return route.StreamIdParts.Select(part => MappingFailure(part.Value, part.Type, command, model, authoring)).FirstOrDefault(failure => failure is not null);
        }
        if (route.StreamIdType is not { } type)
        {
            return route.StreamId is null && route.StreamIdLiteral is null ? null : "an unkeyed stream cannot carry a stream-id mapping (PLAY0273)";
        }
        if ((route.StreamId is null) == (route.StreamIdLiteral is null))
        {
            return "a scalar stream id requires exactly one complete mapping (PLAY0273)";
        }

        return MappingFailure(route.StreamIdLiteral ?? (MappingSourceModel)new PropertyPathSource(route.StreamId!), type, command, model, authoring);
    }

    static string? MappingFailure(MappingSourceModel value, TypeReferenceModel type, CommandModel command, ApplicationModel model, bool authoring)
    {
        var primitive = model.Concepts.SingleOrDefault(concept => concept.Name == type.Name)?.Primitive.ToString() ?? type.Name;
        if (type.IsOptional || type.IsCollection || (primitive is not ("String" or "Uuid") &&
            !(primitive == "Int" && model.Concepts.Any(concept => concept.Name == type.Name))))
        {
            return "stream ids require required scalar text, UUID or integer-backed concept types (PLAY0273)";
        }
        if (value is PropertyPathSource path)
        {
            if (path.Path.Contains('.', StringComparison.Ordinal))
            {
                return authoring ? null : "property-path route mappings are not admitted (PLAY0268)";
            }
            if (command.Authoring?.Generated.Any(property => property.Name == path.Path) == true)
            {
                return "generated-property route mappings occur before generation (PLAY0273)";
            }

            return command.Properties.Any(property => property.Name == path.Path && property.Type == type) ? null :
                "a route mapping must read a direct required command property of its declared type (PLAY0273)";
        }
        if (value is LiteralSource literal && LiteralFits(literal, type, model))
        {
            return null;
        }

        return "a route mapping must supply a portable literal or direct command input (PLAY0273)";
    }

    static bool LiteralFits(LiteralSource literal, TypeReferenceModel type, ApplicationModel model)
    {
        var primitive = model.Concepts.SingleOrDefault(concept => concept.Name == type.Name)?.Primitive.ToString() ?? type.Name;

        if (type.IsOptional || type.IsCollection)
        {
            return false;
        }

        return primitive switch
        {
            "String" => literal.Value is string text && text.Length > 0 && text.IsNormalized() && new ScreenplayNaming().ToStringLiteral(text) == text,
            "Uuid" => literal.Value is string uuid && Guid.TryParse(uuid, out _),
            "Int" => literal.Value is int || (literal.Value is long number && number is >= -9007199254740991 and <= 9007199254740991),
            _ => false
        };
    }
}
