// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.Execution;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Execution;

namespace Cratis.Arc;

/// <summary>
/// Represents the options for Arc.
/// </summary>
public class ArcOptions
{
    bool _trustForwardedIdentityHeaders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcOptions"/> class.
    /// </summary>
    public ArcOptions()
    {
        JsonSerializerOptions = new JsonSerializerOptions().ConfigureArcDefaults(Internals.DerivedTypesOrDefault);
    }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> configured for Arc.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Gets or sets the options for the correlation ID.
    /// </summary>
    public CorrelationIdOptions CorrelationId { get; set; } = new();

    /// <summary>
    /// Gets or sets the options for the tenancy.
    /// </summary>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <summary>
    /// Gets or sets what type of identity details provider to use. If none is specified it will use type discovery to try to find one.
    /// </summary>
    public Type? IdentityDetailsProvider { get; set; }

    /// <summary>
    /// Gets or sets the options for generated API endpoints (commands and queries).
    /// </summary>
    public ApiEndpointOptions GeneratedApis { get; set; } = new();

    /// <summary>
    /// Gets or sets the exposure options for command and query introspection.
    /// </summary>
    public IntrospectionOptions Introspection { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the host trusts the Microsoft Identity Platform identity headers
    /// (<c>x-ms-client-principal</c>, <c>x-ms-client-principal-id</c> and <c>x-ms-client-principal-name</c>) that a
    /// trusted ingress forwards. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These headers are not signed: anyone who can reach the application directly can set them. Arc therefore
    /// ignores them, and treats the request as anonymous, unless the host opts in. Enable this only when every
    /// request reaches the application through an ingress that strips client-supplied identity headers and sets its
    /// own, such as Azure App Service or Container Apps authentication (EasyAuth) or Cratis AuthProxy.
    /// </para>
    /// <para>
    /// Bound from <c>Cratis:Arc:TrustForwardedIdentityHeaders</c>. The obsolete
    /// <c>Cratis:Arc:Introspection:TrustForwardedIdentityHeaders</c> setting also turns it on.
    /// </para>
    /// </remarks>
    public bool TrustForwardedIdentityHeaders
    {
#pragma warning disable CS0618 // Type or member is obsolete - the introspection setting is honored as an alias
        get => _trustForwardedIdentityHeaders || (Introspection?.TrustForwardedIdentityHeaders ?? false);
#pragma warning restore CS0618
        set => _trustForwardedIdentityHeaders = value;
    }

    /// <summary>
    /// Gets or sets the options for observable queries.
    /// </summary>
    public QueryOptions Query { get; set; } = new();

    /// <summary>
    /// Gets or sets the hosting options for Arc, only used by Arc.Core.
    /// </summary>
    public HostingOptions Hosting { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether exception detail (messages and stack traces) is exposed
    /// to clients in serialized <see cref="Commands.CommandResult"/> and <see cref="Queries.QueryResult"/> responses.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/> only in the Development environment and <see langword="false"/> otherwise.
    /// When <see langword="false"/>, exception messages and stack traces are redacted from responses (the full detail
    /// is still logged server-side and the correlation identifier is retained) to avoid leaking internal information.
    /// </remarks>
    public bool ExposeExceptionDetails { get; set; } = RuntimeEnvironment.IsDevelopment;

    /// <summary>
    /// Add a type info resolver, typically the application's source-generated <see cref="JsonSerializerContext"/>, to the
    /// <see cref="JsonSerializerOptions"/> Arc serializes with.
    /// </summary>
    /// <param name="resolver">The <see cref="IJsonTypeInfoResolver"/> to add, for instance <c>MyJsonSerializerContext.Default</c>.</param>
    /// <returns>The <see cref="ArcOptions"/> for continuation.</returns>
    /// <remarks>
    /// <para>
    /// Resolvers added here are consulted first, so the application's contracts - including any it customizes for Arc's own
    /// types - win. That holds even after the application has wrapped <see cref="JsonSerializerOptions"/>'s
    /// <see cref="System.Text.Json.JsonSerializerOptions.TypeInfoResolver"/>, for instance with <c>WithAddedModifier</c>,
    /// or cleared it: the resolver then goes ahead of the wrapper, or ahead of Arc's resolver composed anew. Resolvers
    /// added while Arc's resolver is still in the chain are consulted in the order they were added; once the resolver has
    /// been wrapped, a resolver added later is consulted before earlier ones. Resolvers appended to <see cref="JsonSerializerOptions"/>'s
    /// <see cref="System.Text.Json.JsonSerializerOptions.TypeInfoResolverChain"/> come next, then Arc's source-generated
    /// metadata for its own wire types, and the reflection-based resolver comes last, only when reflection-based
    /// serialization is enabled. That lets an application that is trimmed or compiled with NativeAOT, where reflection is
    /// disabled, serialize its read models, query arguments and command responses through generated metadata. A command
    /// response is written as <c>CommandResult&lt;TResponse&gt;</c>, which Arc's metadata cannot cover, so the
    /// application's context has to include <c>CommandResult&lt;TResponse&gt;</c> for each response type.
    /// </para>
    /// <para>
    /// Add resolvers while configuring Arc, before the first serialization: <see cref="JsonSerializerOptions"/> become
    /// read-only once used.
    /// </para>
    /// </remarks>
    public ArcOptions AddJsonTypeInfoResolver(IJsonTypeInfoResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        JsonSerializerOptions.AddTypeInfoResolverBeforeArcDefaults(resolver);
        return this;
    }
}
