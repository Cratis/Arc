// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when a filter-supplied subscription scope cannot be serialized and restored.
/// </summary>
/// <param name="scopeType">The type of the supplied scope.</param>
/// <param name="innerException">The serialization failure, if available.</param>
public class InvalidSubscriptionScope(Type scopeType, Exception? innerException = null)
    : Exception($"Subscription scope of type '{scopeType.FullName}' cannot be represented as a serializable snapshot.", innerException);
