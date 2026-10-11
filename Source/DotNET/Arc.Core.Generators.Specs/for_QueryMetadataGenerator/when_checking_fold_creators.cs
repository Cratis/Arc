// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator;

public class when_checking_fold_creators
{
    [Theory]
    [InlineData("public static Order Create(Placed e) => new();", false)]
    [InlineData("public static Order Create(Derived e) => new();", false)]
    [InlineData("public static Order Create(EventContext context) => new();", false)]
    [InlineData("public static Order Create(EventContext? context) => new();", false)]
    [InlineData("public static Task<Order> Create(Placed e) => Task.FromResult(new Order());", false)]
    [InlineData("public static ValueTask<Order> Create(Placed e) => ValueTask.FromResult(new Order());", false)]
    [InlineData("[NotAQuery] public static Order Create(Unmarked e) => new();", false)]
    [InlineData("public static Order Create(OrderId id) => new();", true)]
    [InlineData("public static Order Create(Unmarked e) => new();", true)]
    [InlineData("public static Order From(Placed e) => new();", true)]
    [InlineData("public static Order Create(Placed e, int first, int second) => new();", true)]
    [InlineData("public static IEnumerable<Order> Create(Placed e) => [];", true)]
    public void should_register_only_real_queries(string method, bool expected)
    {
        var result = GeneratorTestHelper.RunGenerator(Source(method));
        var generated = GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs");
        result.Diagnostics.ShouldBeEmpty();
        generated.Contains($"TestApp.Order.{(method.Contains(" From(", StringComparison.Ordinal) ? "From" : "Create")}", StringComparison.Ordinal).ShouldEqual(expected);
        generated.ShouldContain("TestApp.Order.ById");
    }

    [Fact]
    public void should_register_an_overloaded_query_once()
    {
        var result = GeneratorTestHelper.RunGenerator(Source("""
            public static Order Create(Placed e) => new();
            public static Order Create(string id) => new();
            """));
        var generated = GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs");
        generated.Split("TestApp.Order.Create", StringSplitOptions.None).Length.ShouldEqual(2);
    }

    static string Source(string method) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        namespace Cratis.Chronicle.Events
        {
            public struct EventContext { }
            [AttributeUsage(AttributeTargets.Class, Inherited = true)]
            public class EventTypeAttribute : Attribute { }
        }
        namespace TestApp
        {
            [EventType] public class Placed { }
            public class Derived : Placed { }
            public class Unmarked { }
            public record OrderId(string Value) : ConceptAs<string>(Value);
            [ReadModel]
            public class Order
            {
                public static Order ById(string id) => new();
                {{method}}
            }
        }
        """;
}
