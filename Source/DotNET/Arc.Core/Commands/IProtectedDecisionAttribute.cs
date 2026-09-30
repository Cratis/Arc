// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Implemented by an attribute that marks a command as an explicitly protected decision command.
/// </summary>
/// <remarks>
/// Only attributes on the command type are honoured. Implementing this interface on the command type itself has no effect.
/// </remarks>
public interface IProtectedDecisionAttribute;
