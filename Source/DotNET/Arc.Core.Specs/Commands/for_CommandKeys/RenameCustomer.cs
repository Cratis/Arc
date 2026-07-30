// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Commands.for_CommandKeys;

public record RenameCustomer([property: Key] Guid CustomerId, string NewName);
