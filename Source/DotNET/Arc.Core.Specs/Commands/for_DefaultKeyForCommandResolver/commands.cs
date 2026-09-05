// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_DefaultKeyForCommandResolver;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator Guid(CustomerId id) => id.Value;
}

public record RenameCustomer([property: Key] Guid CustomerId, string NewName);

public record RenameCustomerByConcept([property: Key] CustomerId CustomerId, string NewName);

public record RenameCustomerByNumber([property: Key] int CustomerNumber, string NewName);

public record MoveItem(Guid CartId, Guid ItemId) : ICanProvideKeyForCommand
{
    public string GetKey() => $"{CartId}/{ItemId}";
}

public record ComposesNothing(Guid CartId) : ICanProvideKeyForCommand
{
    public string? GetKey() => null;
}

public record CarriesNoKey(string Name);

public record CarriesAnUnsetKey([property: Key] string? CustomerId);

#pragma warning restore SA1402, SA1649
