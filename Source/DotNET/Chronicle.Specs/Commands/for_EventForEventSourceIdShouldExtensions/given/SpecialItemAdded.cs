// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.given;

public record SpecialItemAdded(int Quantity) : ItemAdded(Quantity);
