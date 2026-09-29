// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Values of Arc's wire types exercising what their JSON depends on: the Arc naming policy, ignoring null values, the
/// concept, enum and date converters, property level converters, <see cref="object"/> typed members carrying the
/// application's own types, and characters the encoder escapes.
/// </summary>
public static class representative_wire_values
{
    public static readonly CorrelationId CorrelationId = new(new Guid(0x3f0d9c6e, 0x5a8b, 0x4c1d, 0x9e, 0x2f, 0x7a, 0x6b, 0x5c, 0x4d, 0x3e, 0x2f));

    public static readonly OrderReadModel Order = new(
        new OrderNumber("R&D <Ærlig> \"42\""),
        OrderState.Shipped,
        new DateOnly(2026, 9, 28),
        null,
        12.5m,
        [new OrderLine("SKU-1", 2), new OrderLine("SKU-2", 1)]);

    public static readonly OrderReadModel OtherOrder = Order with { Number = new OrderNumber("other"), State = OrderState.Pending };

    public static ChangeSet ChangeSet => new()
    {
        Added = [Order],
        Replaced = [OtherOrder],
        Removed = [new OrderNumber("removed")],
    };

    public static QueryResult QueryResult => new()
    {
        CorrelationId = CorrelationId,
        Data = new[] { Order, OtherOrder },
        Paging = new PagingInfo(2, 10, 42),
        ValidationResults =
        [
            ValidationResult.Warning("Check <this>", ["number"], new OrderLine("SKU-3", 3), ValidationResultReason.ConcurrencyViolation, "detail"),
        ],
        ExceptionMessages = ["Something æøå happened"],
        ExceptionStackTrace = "at Somewhere()",
        ChangeSet = ChangeSet,
    };

    public static CommandResult CommandResult => new()
    {
        CorrelationId = CorrelationId,
        IsAuthorized = false,
        AuthorizationFailureReason = "Not <allowed>",
        ValidationResults = [ValidationResult.Error("Invalid", ["number"])],
        ExceptionMessages = ["Failed"],
        ExceptionStackTrace = "at Handle()",
    };

    public static CommandResult<OrderReadModel> CommandResultWithResponse => new(CorrelationId, Order);

    /// <summary>
    /// Creates the options Arc serialized with before it composed a type info resolver chain: its defaults, with
    /// every type resolved through reflection - the resolver <see cref="JsonSerializer"/> falls back to for options
    /// that have none.
    /// </summary>
    /// <returns>The <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions OptionsAsBefore()
    {
        var options = new JsonSerializerOptions().ConfigureArcDefaults();
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        return options;
    }
}

public record OrderNumber(string Value) : ConceptAs<string>(Value);

public enum OrderState
{
    Pending = 0,
    Shipped = 1
}

public record OrderLine(string Sku, int Quantity);

public record OrderReadModel(OrderNumber Number, OrderState State, DateOnly PlacedOn, string? Note, decimal Total, IEnumerable<OrderLine> Lines);

#pragma warning restore SA1402 // File may only contain a single type
