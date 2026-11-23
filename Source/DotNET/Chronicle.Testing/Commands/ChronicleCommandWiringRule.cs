// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>Checks Chronicle lookup declarations, never acquiring state or invoking a key provider.</summary>
/// <param name="artifacts">The application's explicitly configured Chronicle artifacts.</param>
public sealed class ChronicleCommandWiringRule(IClientArtifactsProvider artifacts) : ICommandWiringRule
{
    readonly HashSet<Type> _models = ReadModelServiceCollectionExtensions.DiscoverReadModelTypes(artifacts).ToHashSet();

    /// <inheritdoc/>
    public IEnumerable<Type> CoveredResolverTypes => [typeof(ChronicleParameterDependencyResolver)];

    /// <inheritdoc/>
    public CommandWiringVerdict Inspect(CommandWiringParameter parameter)
    {
        try
        {
            var dependency = parameter.Parameter.ParameterType;
            if (dependency.IsGenericType && dependency.GetGenericTypeDefinition() == typeof(DecisionRead<>))
            {
                return InspectToken(parameter);
            }
            if (PerStreamReadModelBinding.IsDeclared(dependency))
            {
                return InspectStream(parameter);
            }
            if (IsModelBacking(dependency, "MongoDB.Driver.IMongoCollection`1"))
            {
                return InspectCollection(parameter, dependency.GenericTypeArguments[0]);
            }
            if (IsModelBacking(dependency, "System.Linq.IQueryable`1"))
            {
                return new(true, "unsupported", "Queryable provider activation requires a provider-backed scenario; no model query will be created.");
            }
            return _models.Contains(dependency) ? InspectLookup(parameter) : CommandWiringVerdict.NotApplicable;
        }
        catch (Exception failure)
        {
            return new(true, "lookup", "Verify the Chronicle lookup declaration and generated key metadata; inspect Cause for the exception type.", failure);
        }
    }

    static CommandWiringVerdict InspectCollection(CommandWiringParameter parameter, Type model)
    {
        if (parameter.ReadProtection != ReadProtectionMode.On)
        {
            return CommandWiringVerdict.NotApplicable;
        }
        var advisory = parameter.Parameter.GetCustomAttribute<UnprotectedAttribute>() ?? model.GetCustomAttribute<UnprotectedAttribute>();
        return advisory is not null
            ? CommandWiringVerdict.NotApplicable
            : new(true, "refused", $"Chronicle cannot protect {model.FullName} through IMongoCollection; inject the read model or declare an explicit advisory read.");
    }

    static CommandWiringVerdict InspectToken(CommandWiringParameter parameter) =>
        HasLookup(parameter.Parameter)
            ? new(true, "lookup", "DecisionRead tokens cannot be redirected by a lookup declaration.")
            : new(true, "unsupported", "DecisionRead factory provenance and admission require an invocation; use DecisionReadAdmissions and a command scenario.");

    static CommandWiringVerdict InspectStream(CommandWiringParameter parameter) =>
        HasLookup(parameter.Parameter)
            ? new(true, "lookup", "Per-stream models belong to the command's own stream and cannot be redirected.")
            : new(true, "unsupported", "Verify the stream route and template values in a command scenario; assembly metadata cannot prove the resolved route.");

    static bool HasLookup(ParameterInfo parameter) => LookUpByKeyProviderDeclarations.For(parameter).Any();

    static CommandWiringVerdict InspectLookup(CommandWiringParameter parameter)
    {
        if (ReadModelKeyProviders.Validate(parameter.Parameter, parameter.CommandType) is not null)
        {
            return CommandWiringVerdict.Supported;
        }
        var lookup = parameter.Parameter.GetCustomAttribute<LookUpByAttribute>();
        if (lookup is null && parameter.ReadProtection != ReadProtectionMode.On)
        {
            // Off/Measure fall back to the event source, which may be generated without an id property.
            return CommandWiringVerdict.Supported;
        }
        var configuration = parameter.Services.GetService<IConfiguration>();
        var properties = CommandReadModelKeys.PropertiesFor(parameter.CommandType, configuration, null);
        if (lookup is not null)
        {
            return properties.Any(property => property.Name == lookup.Property)
                ? CommandWiringVerdict.Supported
                : new(true, "lookup", $"Declare a readable command property named {lookup.Property}.");
        }
        var key = CommandReadModelKeys.KeyTypeFor(parameter.Parameter.ParameterType, configuration, null);
        return properties.Count(property => property.PropertyType == key) > 1
            ? new(true, "lookup", "Several command properties match the model key type; declare one explicit lookup.")
            : CommandWiringVerdict.Supported;
    }

    bool IsModelBacking(Type dependency, string definition) => dependency.IsGenericType &&
        dependency.GetGenericTypeDefinition().FullName == definition && _models.Contains(dependency.GenericTypeArguments[0]);
}
