// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Contexts;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The caller and resource presented to an authorization policy.
/// </summary>
/// <param name="Principal">The principal selected by the authorization schemes.</param>
/// <param name="Target">The command type or query method being authorized.</param>
/// <param name="Resource">The command or query context.</param>
public record AuthorizationPolicyContext(ClaimsPrincipal Principal, MemberInfo Target, object Resource)
{
    /// <summary>
    /// Gets the time Arc received the operation being authorized, not the network arrival time.
    /// A hand-constructed context has <see langword="default"/> unless the caller sets this property.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// Gets the values the model adds to <c>$context</c> with <c>provide</c>, resolved lazily, once, for the request.
    /// </summary>
    /// <remarks>
    /// Read them by name, such as <c>context.Values.Vessel</c>. An absent value is <see langword="null"/>, which is unknown.
    /// A policy that is evaluated outside a request reads every value as absent.
    /// </remarks>
    public dynamic Values => ProvidedValues;

    /// <summary>
    /// Gets the provided values with the asynchronous and typed reads of <see cref="ContextValues"/>.
    /// </summary>
    public ContextValues ProvidedValues => ContextValues.For(Services, () => Target as Type ?? Target.DeclaringType, RequestCancellation);

    /// <summary>
    /// Gets or sets the services scoped to the request being authorized.
    /// </summary>
    internal IServiceProvider? Services { get; init; }

    /// <summary>
    /// Gets or sets the cancellation token of the request being authorized.
    /// </summary>
    internal CancellationToken RequestCancellation { get; init; }

    /// <summary>
    /// Gets the ambient principal accessor for per-policy identity checks when evaluating a pipeline verdict.
    /// It is evaluation plumbing, not policy input, so it takes no part in value equality.
    /// </summary>
    internal ICurrentPrincipalAccessor? PrincipalAccessor { get; init; }

    /// <summary>
    /// Compares the public policy input: principal, target, resource and received time.
    /// </summary>
    /// <param name="other">The context to compare with.</param>
    /// <returns>Whether both contexts present the same policy input.</returns>
    public virtual bool Equals(AuthorizationPolicyContext? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (EqualityContract == other.EqualityContract &&
          EqualityComparer<ClaimsPrincipal>.Default.Equals(Principal, other.Principal) &&
          EqualityComparer<MemberInfo>.Default.Equals(Target, other.Target) &&
          EqualityComparer<object>.Default.Equals(Resource, other.Resource) &&
          ReceivedAt == other.ReceivedAt));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(EqualityContract, Principal, Target, Resource, ReceivedAt);

    /// <summary>
    /// Gets the provided values as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">A type whose public properties, or constructor parameters, are named after the provided values.</typeparam>
    /// <returns>The provided values as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextValuesTypeMismatch">The provided values do not fit <typeparamref name="T"/>.</exception>
    public T ValuesAs<T>() => ProvidedValues.ValuesAs<T>();
}
