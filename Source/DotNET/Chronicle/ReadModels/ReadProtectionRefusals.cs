// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Maps Chronicle admission reasons to parameter-specific configuration errors.
/// </summary>
internal static class ReadProtectionRefusals
{
    /// <summary>
    /// Names the rejected parameter and an actionable alternative.
    /// </summary>
    /// <param name="commandType">The executing command.</param>
    /// <param name="parameter">The injected parameter.</param>
    /// <param name="reason">The Chronicle admission refusal.</param>
    /// <returns>The parameter-specific error.</returns>
    internal static ReadCannotBeProtected For(Type commandType, ParameterInfo parameter, DecisionReadRefusalReason reason)
    {
        var model = parameter.ParameterType;
        var (description, fix) = reason switch
        {
            DecisionReadRefusalReason.Reducer => ("through a reducer", "read a projection of the same events, or wait for reducer support"),
            DecisionReadRefusalReason.ReducerDependencies => ("through a reducer with constructor dependencies", "remove constructor dependencies from the reducer"),
            DecisionReadRefusalReason.ReducerFilters => ("through a reducer that filters its events", "remove the reducer's event filter or read an unfiltered projection"),
            DecisionReadRefusalReason.Join or DecisionReadRefusalReason.Hierarchy => ("with joins or children", "read the parts as separate models"),
            DecisionReadRefusalReason.FromEventProperty or DecisionReadRefusalReason.NotEventSourceKeyed => ("keyed by an event property", "append a fact to the entity it is keyed by, or use a constraint"),
            DecisionReadRefusalReason.GenerationDeliveryNotSupported => ("using Pinned generation delivery", "use Compatibility delivery for this projection"),
            DecisionReadRefusalReason.KeyConversion => ("with an unsupported key type", $"give {model.Name} a string or Guid Id"),
            DecisionReadRefusalReason.StreamKeyRequiresStreamScope => ("keyed by a stream", "use IDecisionReads.Get with an explicit source, stream type and stream id in Provide"),
            DecisionReadRefusalReason.NotEventLog => ("from another event sequence", "read a model of the event log"),
            DecisionReadRefusalReason.AmbiguousProjection => ("without exactly one backing definition", "register exactly one projection or admitted reducer for the model"),
            DecisionReadRefusalReason.DefinitionMismatch => ("with a mismatched registered definition", "register the current definition before executing commands"),
            _ => ($"with unsupported shape {reason}", "read a source-keyed model with a closed set of event types")
        };

        return new(commandType, parameter.Name!, model, description, fix);
    }
}
