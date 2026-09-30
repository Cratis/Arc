// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a decision read is attempted without a command pipeline protection profile.
/// </summary>
public class DecisionReadRequiresProtectionProfile() : Exception("Decision reads require a command pipeline protection profile.");
