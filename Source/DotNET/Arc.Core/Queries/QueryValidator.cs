// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the base type for a validator of query.
/// </summary>
/// <typeparam name="T">Type of query.</typeparam>
public class QueryValidator<T> : DiscoverableValidator<T>;
