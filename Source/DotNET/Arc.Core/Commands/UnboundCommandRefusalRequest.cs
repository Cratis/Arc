// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Describes a command whose body could not be bound and that must be refused before any command context exists.
/// </summary>
/// <param name="CommandType">The unbound command type.</param>
/// <param name="BindingFailure">The body-binding failure.</param>
/// <param name="RequestServices">The request services.</param>
/// <param name="ValidateOnly">Whether this is the validation endpoint.</param>
internal record UnboundCommandRefusalRequest(Type CommandType, CommandResult BindingFailure, IServiceProvider RequestServices, bool ValidateOnly);
