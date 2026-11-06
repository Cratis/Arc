// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Connections;
using Cratis.Arc.Identity;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Guards the binding tables against drifting from the option classes: every public readable property reachable from
/// <see cref="ArcOptions"/> in a Cratis type needs a binding entry, and every Cratis type reachable needs a table,
/// except the explicitly listed code-only subtrees.
/// </summary>
public class when_checking_binding_table_coverage : Specification
{
    static readonly Dictionary<Type, ObjectType> _tables = new()
    {
        [typeof(ArcOptions)] = ArcOptionsBinding.Bound,
        [typeof(Execution.CorrelationIdOptions)] = CorrelationIdOptionsBinding.Bound,
        [typeof(Tenancy.TenancyOptions)] = TenancyOptionsBinding.Bound,
        [typeof(ConnectionResolutionOptions)] = ConnectionResolutionOptionsBinding.Bound,
        [typeof(ConnectionResolverOptions)] = ConnectionResolverOptionsBinding.Bound,
        [typeof(TemplateConnectionResolverOptions)] = TemplateConnectionResolverOptionsBinding.Bound,
        [typeof(HttpConnectionResolverOptions)] = HttpConnectionResolverOptionsBinding.Bound,
        [typeof(HttpConnectionResolverAuthenticationOptions)] = HttpConnectionResolverAuthenticationOptionsBinding.Bound,
        [typeof(ApiEndpointOptions)] = ApiEndpointOptionsBinding.Bound,
        [typeof(Introspection.IntrospectionOptions)] = IntrospectionOptionsBinding.Bound,
        [typeof(OnBehalfOfOptions)] = OnBehalfOfOptionsBinding.Bound,
        [typeof(OnBehalfOfSigningKey)] = OnBehalfOfSigningKeyBinding.Bound,
        [typeof(ForwardedIdentityOptions)] = ForwardedIdentityOptionsBinding.Bound,
        [typeof(ForwardedIdentityClientCertificateOptions)] = ForwardedIdentityClientCertificateOptionsBinding.Bound,
        [typeof(ForwardedIdentityKey)] = ForwardedIdentityKeyBinding.Bound,
        [typeof(Calendar.CalendarOptions)] = CalendarOptionsBinding.Bound,
        [typeof(Commands.CommandOptions)] = CommandOptionsBinding.Bound,
        [typeof(Queries.QueryOptions)] = QueryOptionsBinding.Bound,
        [typeof(Queries.QueryHealthOptions)] = QueryHealthOptionsBinding.Bound,
        [typeof(HostingOptions)] = HostingOptionsBinding.Bound,
        [typeof(EndpointGroups.EndpointGroupsOptions)] = EndpointGroupsOptionsBinding.Bound,
        [typeof(System.Text.Json.JsonSerializerOptions)] = JsonSerializerOptionsBinding.Bound
    };

    /// <summary>
    /// Artifacts is a code-only host opt-in, not configuration. Revisit this exemption if that contract changes.
    /// </summary>
    static readonly HashSet<(Type Owner, string Property)> _codeOnlySubtrees =
    [
        (typeof(ArcOptions), nameof(ArcOptions.Artifacts))
    ];

    List<string> _missingMembers;
    List<string> _missingTables;

    void Because()
    {
        _missingMembers = [];
        _missingTables = [];
        var visited = new HashSet<Type>();
        Visit(typeof(ArcOptions), visited);
        Visit(typeof(System.Text.Json.JsonSerializerOptions), visited);
    }

    [Fact] void should_have_an_entry_for_every_property() => _missingMembers.ShouldBeEmpty();

    [Fact] void should_have_a_table_for_every_reachable_type() => _missingTables.ShouldBeEmpty();

    [Fact]
    void should_exempt_only_existing_unbound_properties()
    {
        foreach (var (owner, property) in _codeOnlySubtrees)
        {
            owner.GetProperty(property).ShouldNotBeNull();
            _tables[owner].MemberNames.ShouldNotContain(property);
        }
    }

    [Fact]
    void should_keep_artifacts_without_public_setters() =>
        typeof(ArtifactsOptions).GetProperties().Where(_ => _.SetMethod?.IsPublic == true).ShouldBeEmpty();

    void Visit(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return;
        }

        if (!_tables.TryGetValue(type, out var table))
        {
            _missingTables.Add(type.FullName!);
            return;
        }

        var bound = table.MemberNames.ToHashSet();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(_ => !_codeOnlySubtrees.Contains((type, _.Name))))
        {
            if (property.GetMethod is null || property.GetMethod.GetParameters().Length != 0)
            {
                continue;
            }

            if (!bound.Contains(property.Name))
            {
                _missingMembers.Add($"{type.FullName}.{property.Name}");
            }

            foreach (var candidate in Candidates(property.PropertyType))
            {
                Visit(candidate, visited);
            }
        }
    }

    static IEnumerable<Type> Candidates(Type type)
    {
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().SelectMany(Candidates);
        }

        var isOptionsType = type.IsClass && !type.IsAbstract && type.Namespace?.StartsWith("Cratis.Arc", StringComparison.Ordinal) == true;
        return isOptionsType ? [type] : [];
    }
}
