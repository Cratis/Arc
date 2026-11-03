// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Marks comparisons in the linked scenarios without a dependency on xUnit.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : Attribute;
