// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a detached decision read is attempted inside an executing command.
/// </summary>
public class DetachedDecisionReadRefused() : Exception("Detached decision reads cannot be used inside an executing command.");
