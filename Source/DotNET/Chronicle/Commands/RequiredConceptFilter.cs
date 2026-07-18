// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command filter that rejects a command carrying a required (non-nullable) concept property that arrived
/// null, before argument resolution and the handler run.
/// </summary>
/// <remarks>
/// A concept can never wrap null, so a required concept property that deserialized to null (the client omitted it) is
/// dereferenced later in <c>Provide()</c>, <c>Handle()</c>, or a validator rule and throws a <see cref="NullReferenceException"/>
/// — which surfaces as a redacted server error (HTTP 500) rather than the 400 the input deserves. Surfacing it here as a
/// validation failure turns it into a clean 400 pointing at the offending field. A concept declared nullable
/// (<c>Concept?</c>) is intentionally optional and left alone; the event source key (an <see cref="Cratis.Chronicle.Events.EventSourceId"/>,
/// a generic <c>EventSourceId&lt;T&gt;</c>, or a <c>[Key]</c> property) is excluded because a null key is deliberately
/// resolved to an unspecified id rather than rejected. Only the command's own settable properties are inspected — a
/// computed (get-only) property is never evaluated, so a derived concept that dereferences a missing value cannot turn
/// this into a server error, and a concept nested inside another record is not checked here. To make a concept optional,
/// declare it nullable.
/// </remarks>
[Singleton]
public class RequiredConceptFilter : ICommandFilter
{
    readonly ConcurrentDictionary<Type, PropertyInfo[]> _requiredConceptPropertiesByCommandType = new();

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        var command = context.Command;
        var requiredConceptProperties = _requiredConceptPropertiesByCommandType.GetOrAdd(command.GetType(), RequiredConceptPropertiesFor);
        var missingProperties = requiredConceptProperties
            .Where(property => property.GetValue(command) is null)
            .Select(property => property.Name)
            .ToArray();

        if (missingProperties.Length == 0)
        {
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }

        var result = CommandResult.Success(context.CorrelationId);
        result.MergeWith(new CommandResult
        {
            CorrelationId = context.CorrelationId,
            ValidationResults = [.. missingProperties.Select(name => ValidationResult.Error("The value is required.", [name]))]
        });

        return Task.FromResult(result);
    }

    static PropertyInfo[] RequiredConceptPropertiesFor(Type commandType)
    {
        var nullabilityContext = new NullabilityInfoContext();
        return
        [
            .. commandType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property =>
                    property.CanWrite &&
                    property.GetIndexParameters().Length == 0 &&
                    property.PropertyType.IsConcept() &&
                    !property.IsEventSourceKeyProperty() &&
                    nullabilityContext.Create(property).ReadState == NullabilityState.NotNull)
        ];
    }
}
