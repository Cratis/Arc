// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command receives an <c>IDecisionReads</c> reader that is not command-aware.
/// </summary>
public class ProtectedCommandRequiresCommandAwareReader() : Exception("A protected command requires the command-aware IDecisionReads reader.");
