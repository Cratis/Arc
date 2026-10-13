// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;

namespace Cratis.Arc.Screenplay.for_ReducerReader;

public class when_recognizing_self_shapes
{
    [Theory]
    [InlineData("public static Balance Reduce(Added e, Balance? current) => new();", true)]
    [InlineData("public static Balance? Reduce(Added e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(IAccountEvent e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(UnmarkedBase e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(ExplicitlyRegisteredEvent e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(string e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(string[] e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(System.Action e, Balance? current) => current;", true)]
    [InlineData("public static Balance? Reduce(dynamic e, Balance? current) => current;", false)]
    [InlineData("public static Balance? Reduce(int e, Balance? current) => current;", false)]
    [InlineData("public static Task<Balance> Reduce(Added e, Balance? current) => Task.FromResult(new Balance());", true)]
    [InlineData("public static Task<Balance?> Reduce(Added e, Balance? current, EventContext context) => Task.FromResult(current);", true)]
    [InlineData("#nullable disable\npublic static Balance Reduce(Added e, Balance current) => current;", true)]
    [InlineData("public static Balance Reduce(Added e, Balance current) => current;", false)]
    [InlineData("public Balance Reduce(Added e, Balance? current) => new();", false)]
    [InlineData("private static Balance Reduce(Added e, Balance? current) => new();", false)]
    [InlineData("public static Balance Reduce<T>(Added e, Balance? current) => new();", false)]
    [InlineData("public static Balance Reduce(object e, Balance? current) => new();", false)]
    [InlineData("public static Balance Reduce(Added e, Balance? current, object context) => new();", false)]
    [InlineData("public static Balance Reduce(Added e, Balance? current = null) => new();", false)]
    [InlineData("public static Balance Reduce(Added e, ref Balance? current) => new();", false)]
    [InlineData("public static Task Reduce(Added e, Balance? current) => Task.CompletedTask;", false)]
    public void should_require_the_exact_self_shape(string method, bool expected)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            #nullable enable
            using System.Threading.Tasks;
            using Cratis.Chronicle.Events;
            namespace Library.Feature.Slice;
            [EventType] public record Added;
            public interface IAccountEvent;
            public record UnmarkedBase;
            public record ExplicitlyRegisteredEvent : UnmarkedBase, IAccountEvent;
            public record Balance
            {
                {{method}}
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var model = compilation.GetTypeByMetadataName("Library.Feature.Slice.Balance")!;
        (ReducerReader.ReadModelOf(model) is not null).ShouldEqual(expected);
    }
}
