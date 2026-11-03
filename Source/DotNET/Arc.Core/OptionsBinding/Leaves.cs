// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The leaf types Arc options use, each paired with the converter <c>TypeDescriptor.GetConverter</c> yields for it.
/// </summary>
/// <remarks>
/// The converters are the real ones, so the values converted and the failures raised are those of the reflection binder.
/// Enum converters are created with a literal <see langword="typeof"/> so that trimming keeps what they need.
/// </remarks>
internal static class Leaves
{
    /// <summary>
    /// Gets the leaf type for <see cref="bool"/>.
    /// </summary>
    internal static readonly LeafType Bool = new(typeof(bool), new BooleanConverter(), static () => false);

    /// <summary>
    /// Gets the leaf type for <see cref="int"/>.
    /// </summary>
    internal static readonly LeafType Int = new(typeof(int), new Int32Converter(), static () => 0);

    /// <summary>
    /// Gets the leaf type for <see cref="long"/>.
    /// </summary>
    internal static readonly LeafType Long = new(typeof(long), new Int64Converter(), static () => 0L);

    /// <summary>
    /// Gets the leaf type for <see cref="double"/>.
    /// </summary>
    internal static readonly LeafType Double = new(typeof(double), new DoubleConverter(), static () => 0d);

    /// <summary>
    /// Gets the leaf type for <see cref="char"/>.
    /// </summary>
    internal static readonly LeafType Char = new(typeof(char), new CharConverter(), static () => '\0');

    /// <summary>
    /// Gets the leaf type for <see cref="string"/>.
    /// </summary>
    internal static readonly LeafType String = new(typeof(string), new StringConverter(), null, BclMemberWalks.String);

    /// <summary>
    /// Gets the leaf type for <see cref="TimeSpan"/>.
    /// </summary>
    internal static readonly LeafType TimeSpan = new(typeof(TimeSpan), new TimeSpanConverter(), static () => System.TimeSpan.Zero, BclMemberWalks.TimeSpan);

    /// <summary>
    /// Gets the leaf type for <see cref="Uri"/>.
    /// </summary>
    internal static readonly LeafType Uri = new(typeof(Uri), new UriTypeConverter(), null, BclMemberWalks.Uri);

    /// <summary>
    /// Gets the leaf type for <see cref="UriHostNameType"/>.
    /// </summary>
    internal static readonly LeafType UriHostNameType = new(typeof(UriHostNameType), new EnumConverter(typeof(UriHostNameType)), static () => default(UriHostNameType));

    /// <summary>
    /// Gets the leaf type for <see cref="TenantResolverType"/>.
    /// </summary>
    internal static readonly LeafType TenantResolverType = new(typeof(TenantResolverType), new EnumConverter(typeof(TenantResolverType)), static () => default(TenantResolverType));

    /// <summary>
    /// Gets the leaf type for <see cref="ConnectionResolverType"/>.
    /// </summary>
    internal static readonly LeafType ConnectionResolverType = new(typeof(ConnectionResolverType), new EnumConverter(typeof(ConnectionResolverType)), static () => default(ConnectionResolverType));

    /// <summary>
    /// Gets the leaf type for <see cref="HttpConnectionResolverAuthenticationType"/>.
    /// </summary>
    internal static readonly LeafType HttpConnectionResolverAuthenticationType = new(typeof(HttpConnectionResolverAuthenticationType), new EnumConverter(typeof(HttpConnectionResolverAuthenticationType)), static () => default(HttpConnectionResolverAuthenticationType));

    /// <summary>
    /// Gets the leaf type for <see cref="AzureCredentialType"/>.
    /// </summary>
    internal static readonly LeafType AzureCredentialType = new(typeof(AzureCredentialType), new EnumConverter(typeof(AzureCredentialType)), static () => default(AzureCredentialType));

    /// <summary>
    /// Gets the leaf type for <see cref="ValidationResultSeverity"/>.
    /// </summary>
    internal static readonly LeafType ValidationResultSeverity = new(typeof(ValidationResultSeverity), new EnumConverter(typeof(ValidationResultSeverity)), static () => default(ValidationResultSeverity));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonIgnoreCondition"/>.
    /// </summary>
    internal static readonly LeafType JsonIgnoreCondition = new(typeof(JsonIgnoreCondition), new EnumConverter(typeof(JsonIgnoreCondition)), static () => default(JsonIgnoreCondition));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonNumberHandling"/>.
    /// </summary>
    internal static readonly LeafType JsonNumberHandling = new(typeof(JsonNumberHandling), new EnumConverter(typeof(JsonNumberHandling)), static () => default(JsonNumberHandling));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonObjectCreationHandling"/>.
    /// </summary>
    internal static readonly LeafType JsonObjectCreationHandling = new(typeof(JsonObjectCreationHandling), new EnumConverter(typeof(JsonObjectCreationHandling)), static () => default(JsonObjectCreationHandling));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonCommentHandling"/>.
    /// </summary>
    internal static readonly LeafType JsonCommentHandling = new(typeof(JsonCommentHandling), new EnumConverter(typeof(JsonCommentHandling)), static () => default(JsonCommentHandling));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonUnknownTypeHandling"/>.
    /// </summary>
    internal static readonly LeafType JsonUnknownTypeHandling = new(typeof(JsonUnknownTypeHandling), new EnumConverter(typeof(JsonUnknownTypeHandling)), static () => default(JsonUnknownTypeHandling));

    /// <summary>
    /// Gets the leaf type for <see cref="JsonUnmappedMemberHandling"/>.
    /// </summary>
    internal static readonly LeafType JsonUnmappedMemberHandling = new(typeof(JsonUnmappedMemberHandling), new EnumConverter(typeof(JsonUnmappedMemberHandling)), static () => default(JsonUnmappedMemberHandling));
}
