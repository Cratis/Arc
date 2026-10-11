// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Excludes a static read-model method from model-bound query discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotAQueryAttribute : Attribute;
