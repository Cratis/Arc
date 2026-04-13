// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents HTTP options for observable queries.
/// </summary>
/// <param name="WaitForFirstResult">Whether or not to wait for the first result.</param>
/// <param name="WaitForFirstResultTimeout">The timeout to use while waiting for the first result.</param>
public readonly record struct ObservableQueryHttpOptions(bool WaitForFirstResult, TimeSpan WaitForFirstResultTimeout);
