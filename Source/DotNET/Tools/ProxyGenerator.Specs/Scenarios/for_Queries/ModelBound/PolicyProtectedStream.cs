// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A live read model protected by a policy contributing the Special authentication scheme.
/// </summary>
/// <param name="Value">The emitted value.</param>
[ReadModel]
[Authorize(Policy = "ActiveSubscription")]
public record PolicyProtectedStream(string Value)
{
    static BehaviorSubject<IEnumerable<PolicyProtectedStream>> _subject = new([]);

    /// <summary>
    /// Returns a live subject, including an initial empty snapshot that confirms attachment.
    /// </summary>
    /// <returns>A live subject stream.</returns>
    [Path("/api/policy-protected-stream")]
    public static ISubject<IEnumerable<PolicyProtectedStream>> Watch() => _subject;

    /// <summary>
    /// Emits an item after the subscribe request has completed.
    /// </summary>
    public static void Emit() => _subject.OnNext([new PolicyProtectedStream("later")]);

    /// <summary>
    /// Resets the subject between serialized live-host scenarios.
    /// </summary>
    public static void Reset()
    {
        _subject.Dispose();
        _subject = new BehaviorSubject<IEnumerable<PolicyProtectedStream>>([]);
    }
}
