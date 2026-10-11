// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_a_read_model_has_fold_creators
{
    [Fact]
    public async Task should_not_report_query_diagnostics_for_excluded_methods()
    {
#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.
        const string source = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Cratis.Chronicle.Events
        {
            public struct EventContext { }
            public class EventTypeAttribute : Attribute { }
        }
        namespace TestApp
        {
            [EventType] public class Placed { }
            public class Derived : Placed { }
            [ReadModel]
            public class Order
            {
                public static ValueTask<Order> Create(Placed e) => ValueTask.FromResult(new Order());
                public static ValueTask<Order> Create(Derived e) => ValueTask.FromResult(new Order());
                public static ValueTask<Order> Create(EventContext? context) => ValueTask.FromResult(new Order());
                public static Order Create<T>(Placed e) => new();
                [NotAQuery] public static string Helper() => "helper";
                [NotAQuery] internal static Order Helper<T>() => new();
                public static Order ById(string id) => new();
            }
        }
        """;
#pragma warning restore MA0136
        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
