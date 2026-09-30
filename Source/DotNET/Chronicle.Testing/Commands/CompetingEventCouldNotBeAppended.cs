// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when a competing event could not be appended to the decision scenario log.
/// </summary>
public class CompetingEventCouldNotBeAppended() : Exception("A competing event could not be appended.");
