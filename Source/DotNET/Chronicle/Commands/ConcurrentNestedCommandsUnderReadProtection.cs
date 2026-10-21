// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when sibling commands overlap in an enforcing invocation.
/// </summary>
/// <param name="parent">The owning command.</param>
/// <param name="active">The running nested command.</param>
/// <param name="attempted">The overlapping command.</param>
public class ConcurrentNestedCommandsUnderReadProtection(Type parent, Type active, Type attempted)
    : Exception($"{parent.Name} invoked {attempted.Name} while nested command {active.Name} was still running under read protection. Run nested commands sequentially.");
