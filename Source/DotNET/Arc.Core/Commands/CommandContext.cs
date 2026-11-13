// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Contexts;
using Cratis.Arc.Slices;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the context for a command being executed.
/// </summary>
/// <param name="CorrelationId">The correlation ID for the command.</param>
/// <param name="Type">The type of the command.</param>
/// <param name="Command">The command instance.</param>
/// <param name="Dependencies">The dependencies required to handle the command.</param>
/// <param name="Values">A set of values associated with the command context.</param>
/// <param name="AllowedSeverity">The maximum validation result severity level to allow. Validation results with severity higher than this will cause the command to fail.</param>
/// <param name="Response">The optional response from handling the command, if any.</param>
/// <param name="ServiceProvider">The <see cref="IServiceProvider"/> scoped to the command, used to resolve scoped collaborators such as validators and read models during the command's lifetime.</param>
/// <param name="CancellationToken">The cancellation token for the command execution.</param>
public record CommandContext(
    CorrelationId CorrelationId,
    Type Type,
    object Command,
    IEnumerable<object?> Dependencies,
    CommandContextValues Values,
    ValidationResultSeverity? AllowedSeverity = default,
    object? Response = default,
    IServiceProvider? ServiceProvider = default,
    CancellationToken CancellationToken = default)
{
    readonly SliceLocation? _sliceLocation;
    ClaimsPrincipal? _authorizedPrincipal;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> record.
    /// </summary>
    /// <param name="correlationId">The correlation ID for the command.</param>
    /// <param name="type">The type of the command.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="dependencies">The dependencies required to handle the command.</param>
    /// <param name="values">A set of values associated with the command context.</param>
    /// <param name="allowedSeverity">The maximum validation result severity level to allow. Validation results with severity higher than this will cause the command to fail.</param>
    /// <param name="response">The optional response from handling the command, if any.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the command, used to resolve scoped collaborators such as validators and read models during the command's lifetime.</param>
    public CommandContext(
        CorrelationId correlationId,
        Type type,
        object command,
        IEnumerable<object?> dependencies,
        CommandContextValues values,
        ValidationResultSeverity? allowedSeverity,
        object? response,
        IServiceProvider? serviceProvider)
        : this(correlationId, type, command, dependencies, values, allowedSeverity, response, serviceProvider, default)
    {
    }

    /// <summary>
    /// Gets the time Arc received this command operation. Model-bound dispatch captures it before binding and authorization preparation;
    /// MVC action filters capture it after MVC binding. This is neither network arrival nor time before application middleware.
    /// A hand-constructed context has <see langword="default"/> unless the caller sets this property.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// Gets the values the model adds to <c>$context</c> with <c>provide</c>, resolved lazily, once, for the request.
    /// </summary>
    /// <remarks>
    /// This is what <c>context.Values</c> is on the other contexts. <see cref="Values"/> already carries the values of the command
    /// pipeline, so the provided values are named differently here to keep that contract unchanged. Cast to <see langword="dynamic"/> to read
    /// them by name, such as <c>context.ProvidedValues.Vessel</c>, or use <see cref="ValuesAs{T}"/>.
    /// </remarks>
    public ContextValues ProvidedValues => ContextValues.For(ServiceProvider, () => Type, CancellationToken);

    /// <summary>
    /// Gets the <see cref="Slices.SliceLocation"/> of the command, resolved from the same namespace mapping as its generated proxy.
    /// </summary>
    /// <remarks>
    /// Resolved from <see cref="Type"/> unless set explicitly; controller actions set it from the controller type,
    /// matching the proxy generator. Resolving lazily means every context, including one constructed by hand or by
    /// a test, reports the location of its command type without the caller setting it; resolutions are cached per type.
    /// </remarks>
    public SliceLocation SliceLocation
    {
        get => _sliceLocation ?? SliceLocation.For(Type);
        init => _sliceLocation = value;
    }

    /// <summary>
    /// Gets the validated generated values captured in resolution order. Values must not be logged or traced.
    /// </summary>
    public GeneratedValues GeneratedValues { get; init; } = new();

    /// <summary>
    /// Gets the principal selected by scheme authentication for the rest of this command execution.
    /// </summary>
    public ClaimsPrincipal? AuthorizedPrincipal
    {
        get => _authorizedPrincipal;
        internal set
        {
            _authorizedPrincipal = value;
            AuthorizedPrincipalSnapshot = value is null ? null : AuthorizationPrincipalIdentity.Capture(value);
        }
    }

    /// <summary>
    /// Gets the identity of <see cref="AuthorizedPrincipal"/> as it was when authorization selected it.
    /// </summary>
    internal PrincipalSnapshot? AuthorizedPrincipalSnapshot { get; private set; }

    /// <summary>
    /// Gets the preselected policy and scheme plan when a command must establish identity before its execution scopes.
    /// </summary>
    internal PreparedAuthorization? PreparedAuthorization { get; set; }

    /// <summary>
    /// Gets the successful asynchronous authorization verdict for the final pre-invocation identity check.
    /// </summary>
    internal AuthorizedExecution? AuthorizedExecution { get; set; }

    /// <summary>
    /// Gets whether an Unknown-severity validation failure blocks this command.
    /// </summary>
    internal bool BlockUnknownValidationSeverity { get; init; }

    /// <summary>
    /// Gets whether this command is a controller action, so commands it runs through the command pipeline own their own transaction.
    /// </summary>
    internal bool IsControllerAction { get; init; }

    /// <summary>
    /// Gets the provided values as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">A type whose public properties, or constructor parameters, are named after the provided values.</typeparam>
    /// <returns>The provided values as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextValuesTypeMismatch">The provided values do not fit <typeparamref name="T"/>.</exception>
    public T ValuesAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>() => ProvidedValues.ValuesAs<T>();
}
