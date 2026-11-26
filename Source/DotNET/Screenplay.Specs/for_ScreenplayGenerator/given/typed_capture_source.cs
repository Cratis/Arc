// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// The reduced, Stage-shaped source of a stateful once-only capture over a foreign public event.
/// </summary>
/// <remarks>
/// The reactor reproduces what Stage renders for the V32/V33 order-intake golden models (Stage 1fa5f639, Screenplay
/// 89ad9a11): an identity deriving from <c>EventSourceId&lt;Guid&gt;</c>, a foreign event carrying its origin, a
/// local event, the capture reactor and the projection consuming the local event. The text is embedded so no
/// specification depends on a path outside this repository.
/// </remarks>
public static class typed_capture_source
{
    /// <summary>The path of the capture slice.</summary>
    public const string ReceivePath = "Library/Sales/Orders/ReceiveOrders/";

    /// <summary>The path of the consuming projection slice.</summary>
    public const string StatePath = "Library/Sales/Orders/OrderState/";

    /// <summary>
    /// Builds the files of the reduced application.
    /// </summary>
    /// <param name="keyIsEventSource">Whether the capture declares its key to be the event source (V33).</param>
    /// <param name="mutate">Optionally changes the reactor text, to prove a near miss is refused.</param>
    /// <returns>The source files.</returns>
    public static (string Path, string Text)[] Files(bool keyIsEventSource, Func<string, string>? mutate = null) =>
    [
        ("Library/Sales/Orders/OrderId.cs", Identity),
        (ReceivePath + "OrderPlaced.cs", Placed),
        (ReceivePath + "OrderReceived.cs", Received),
        (ReceivePath + "OrderIntake.cs", (mutate ?? (text => text))(Reactor(keyIsEventSource))),
        (StatePath + "OrderStateEntry.cs", State)
    ];

    /// <summary>
    /// Gets the text of the capture reactor.
    /// </summary>
    /// <param name="keyIsEventSource">Whether the capture declares its key to be the event source (V33).</param>
    /// <returns>The reactor source.</returns>
    public static string Reactor(bool keyIsEventSource)
    {
        var sourceCheck = keyIsEventSource
            ? """
              if (!global::System.String.Equals(global::System.Convert.ToString(ConceptValue(key) ?? key, global::System.Globalization.CultureInfo.InvariantCulture), context.EventSourceId.Value, global::System.StringComparison.Ordinal)) throw new CaptureKeyIsNotTheEventSource();
              """
            : string.Empty;
        var sourceFilter = keyIsEventSource ? "eventSourceId: context.EventSourceId, " : string.Empty;
        var sourceClass = keyIsEventSource
            ? "sealed class CaptureKeyIsNotTheEventSource() : global::System.Exception(\"Capture 'OrderIntake' key 'orderId' does not match the delivered event source.\");"
            : string.Empty;

        return $$"""
            using System.Collections.Generic;
            using System.Linq;

            namespace Library.Sales.Orders.ReceiveOrders;

            [global::Cratis.Chronicle.Events.EventStoreAttribute("storefront")]
            [global::Cratis.Chronicle.Reactors.ReactorAttribute("0b7d6b3e-1b3a-4f55-9d0c-6c0c5a7c9a11")]
            [global::Cratis.Chronicle.Reactors.OnceOnlyAttribute]
            public class OrderIntake(global::Cratis.Chronicle.IEventStore eventStore) : global::Cratis.Chronicle.Reactors.IReactor
            {
                // Each effect is keyed by its delivery; a kernel without idempotent appends refuses the key rather than appending twice.
                public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<object>> OnOrderPlaced(global::Library.Sales.Orders.ReceiveOrders.OrderPlaced @event, global::Cratis.Chronicle.Events.EventContext context, global::Cratis.Chronicle.Reactors.ReactorDelivery delivery)
                {
                    var effects = new global::System.Collections.Generic.List<object>();
                    var observed = Observe(@event)!;
                    var previous = await Previous(observed, context);
                    var current = Overlay(previous, observed);
                    if (previous is not null && SameState(previous, current)) return effects;
                    effects.Add(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.OrderId, new global::Library.Sales.Orders.ReceiveOrders.OrderReceived(Field<global::Library.Sales.Orders.OrderId>(current, "orderId", required: true))) { IdempotencyKey = delivery.IdempotencyKeyFor("0b7d6b3e-1b3a-4f55-9d0c-6c0c5a7c9a11/0") });
                    return effects;
                }

                async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<string, object?>?> Previous(global::System.Collections.Generic.Dictionary<string, object?> observed, global::Cratis.Chronicle.Events.EventContext context)
                {
                    var key = observed.GetValueOrDefault("orderId");
                    if (key is null || (ConceptValue(key) ?? key) is string text && string.IsNullOrWhiteSpace(text)) throw new EmptyCaptureKey();
                    {{sourceCheck}}
                    var inbox = eventStore.GetEventSequence(global::Cratis.Chronicle.Reactors.ReactorTypeExtensions.GetEventSequenceId(typeof(OrderIntake)));
                    var history = await inbox.GetFromSequenceNumber(global::Cratis.Chronicle.Events.EventSequenceNumber.First, {{sourceFilter}}filterEventTypes: [eventStore.EventTypes.GetEventTypeFor(typeof(global::Library.Sales.Orders.ReceiveOrders.OrderPlaced))]);
                    global::System.Collections.Generic.Dictionary<string, object?>? previous = null;
                    foreach (var fact in history.Where(fact => fact.Context.SequenceNumber.Value < context.SequenceNumber.Value).OrderBy(fact => fact.Context.SequenceNumber.Value))
                    {
                        if (Observe(fact.Content) is { } fields && Same(fields.GetValueOrDefault("orderId"), key)) previous = Overlay(previous, fields);
                    }
                    return previous;
                }

                static global::System.Collections.Generic.Dictionary<string, object?>? Observe(object content) => content switch
                {
                    global::Library.Sales.Orders.ReceiveOrders.OrderPlaced @event => new() { ["orderId"] = @event.OrderId },
                    _ => null
                };

                static global::System.Collections.Generic.Dictionary<string, object?> Overlay(global::System.Collections.Generic.Dictionary<string, object?>? previous, global::System.Collections.Generic.Dictionary<string, object?> observed)
                {
                    var current = previous is null ? new global::System.Collections.Generic.Dictionary<string, object?>() : new global::System.Collections.Generic.Dictionary<string, object?>(previous);
                    foreach (var field in observed) current[field.Key] = field.Value;
                    return current;
                }

                static bool SameState(global::System.Collections.Generic.Dictionary<string, object?> previous, global::System.Collections.Generic.Dictionary<string, object?> current) =>
                    previous.Count == current.Count && current.All(field => previous.TryGetValue(field.Key, out var earlier) && Same(earlier, field.Value));

                static bool Changed(global::System.Collections.Generic.Dictionary<string, object?> previous, global::System.Collections.Generic.Dictionary<string, object?> current, string field) =>
                    previous.ContainsKey(field) != current.ContainsKey(field) ||
                    (previous.ContainsKey(field) && !Same(previous.GetValueOrDefault(field), current.GetValueOrDefault(field)));

                static string Text(object? value)
                {
                    // Frozen .NET 10 conversion: runtime decimal casts differ on .NET 11.
                    static decimal Number(double input, int precision)
                    {
                        var exponent = (int)((global::System.BitConverter.DoubleToUInt64Bits(input) >> 52) & 0x7FF) - 1022;
                        if (exponent < -94) return 0;
                        if (exponent > 96) throw new global::System.OverflowException("Value was either too large or too small for a Decimal.");
                        var (mantissa, scale) = ScaleNumber(global::System.Math.Abs(input), exponent, precision);
                        return mantissa == 0 ? 0 : BuildNumber(mantissa, scale, input < 0, precision);
                    }

                    static (ulong Mantissa, int Scale) ScaleNumber(double input, int exponent, int precision)
                    {
                        global::System.ReadOnlySpan<double> powers = [1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28];
                        var scale = precision - ((exponent * 19728) >> 16);
                        if (scale >= 0)
                        {
                            scale = global::System.Math.Min(scale, 28);
                            input *= powers[scale];
                        }
                        else if (scale != -1 || input >= powers[precision + 1])
                        {
                            input /= powers[-scale];
                        }
                        else
                        {
                            scale = 0;
                        }
                        if (input < powers[precision] && scale < 28)
                        {
                            input *= 10;
                            scale++;
                        }
                        return ((ulong)global::System.Math.Round(input, global::System.MidpointRounding.ToEven), scale);
                    }

                    static decimal BuildNumber(ulong mantissa, int scale, bool negative, int precision)
                    {
                        if (scale < 0)
                        {
                            var result = (decimal)mantissa;
                            for (var index = 0; index < -scale; index++) result *= 10;
                            return negative ? -result : result;
                        }
                        var removable = global::System.Math.Min(scale, precision);
                        while (removable > 0 && mantissa % 10 == 0)
                        {
                            mantissa /= 10;
                            scale--;
                            removable--;
                        }
                        return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, negative, (byte)scale);
                    }

                    return (value is null ? null : ConceptValue(value) ?? value) switch
                    {
                        null => string.Empty,
                        string text => text,
                        bool flag => flag ? "true" : "false",
                        double number => Number(number, 14).ToString("0.############################", global::System.Globalization.CultureInfo.InvariantCulture),
                        float number => Number(number, 6).ToString("0.############################", global::System.Globalization.CultureInfo.InvariantCulture),
                        global::System.IConvertible number => global::System.Convert.ToDecimal(number, global::System.Globalization.CultureInfo.InvariantCulture).ToString("0.############################", global::System.Globalization.CultureInfo.InvariantCulture),
                        _ => string.Empty
                    };
                }

                static object? ConceptValue(object value)
                {
                    for (var type = value.GetType(); type is not null; type = type.BaseType)
                    {
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(global::Cratis.Concepts.ConceptAs<>)) return type.GetProperty("Value")!.GetValue(value);
                    }
                    return null;
                }

                static T Field<T>(global::System.Collections.Generic.Dictionary<string, object?> state, string field, bool required) =>
                    state.GetValueOrDefault(field) is T value ? value : required ? throw new MissingCaptureField(field) : default!;

                sealed class MissingCaptureField(string field) : global::System.Exception("Capture 'OrderIntake' has no value for field '" + field + "' in the state remembered for this key.");
                sealed class EmptyCaptureKey() : global::System.Exception("Capture 'OrderIntake' received an event with an empty key 'orderId'.");

                {{sourceClass}}

                static bool Same(object? left, object? right)
                {
                    if (global::System.Object.ReferenceEquals(left, right)) return true;
                    if (left is null || right is null) return false;
                    if (left is not string && left is global::System.Collections.IEnumerable leftItems && right is global::System.Collections.IEnumerable rightItems)
                    {
                        var first = leftItems.Cast<object?>().ToArray();
                        var second = rightItems.Cast<object?>().ToArray();
                        return first.Length == second.Length && first.Zip(second).All(pair => Same(pair.First, pair.Second));
                    }
                    if (left.GetType() != right.GetType()) return false;
                    if (left is string || left.GetType().IsValueType) return left.Equals(right);
                    return left.GetType().GetProperties().All(property => Same(property.GetValue(left), property.GetValue(right)));
                }
            }
            """;
    }

    const string Identity = """
        namespace Library.Sales.Orders;

        public record OrderId(global::System.Guid Value) : global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>(Value);
        """;

    const string Placed = """
        namespace Library.Sales.Orders.ReceiveOrders;

        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        [global::Cratis.Chronicle.Events.EventStoreAttribute("storefront")]
        public record OrderPlaced(global::Library.Sales.Orders.OrderId OrderId);
        """;

    const string Received = """
        namespace Library.Sales.Orders.ReceiveOrders;

        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        public record OrderReceived(global::Library.Sales.Orders.OrderId OrderId);
        """;

    const string State = """
        using Cratis.Chronicle.Events;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.ModelBound;
        using Library.Sales.Orders.ReceiveOrders;

        namespace Library.Sales.Orders.OrderState;

        [ReadModel]
        [FromEvent<OrderReceived>]
        public record OrderStateEntry(
            [Key, SetFromContext<OrderReceived>(nameof(EventContext.EventSourceId))] Library.Sales.Orders.OrderId OrderId);
        """;
}
