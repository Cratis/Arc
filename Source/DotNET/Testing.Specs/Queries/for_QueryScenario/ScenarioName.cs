// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Testing.for_QueryScenario;

public record ScenarioName(string Value) : ConceptAs<string>(Value);
