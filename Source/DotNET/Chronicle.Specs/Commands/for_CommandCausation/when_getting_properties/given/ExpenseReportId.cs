// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties.given;

public record ExpenseReportId(Guid Value) : ConceptAs<Guid>(Value);
