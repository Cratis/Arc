// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a command with enrolled decision reads appends returned events outside its transaction.
/// </summary>
public class ReturnedEventsCannotBeAppendedOutsideDecisionTransaction() : Exception("A command with enrolled decision reads cannot append returned events outside its transaction.");
