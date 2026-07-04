// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Fakes;

/// <summary>
/// Stands in for a Chronicle service that only WithChronicle() would register, so leaving it unregistered
/// reproduces the half-configured Chronicle failure.
/// </summary>
public class FakeEventStoreName;
