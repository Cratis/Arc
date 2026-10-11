// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

#pragma warning disable SA1402 // Related query-discovery fixtures.
#pragma warning disable SA1649 // Related query-discovery fixtures.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

[EventType]
public record Placed;
public record Derived : Placed;
public record Unmarked;
public record OrderId(string Value) : ConceptAs<string>(Value);

[ReadModel]
public record FoldReadModel
{
    public static FoldReadModel Create(Placed e) => new();
    public static FoldReadModel Create(Derived e) => new();
    public static FoldReadModel Create(EventContext context) => new();
    public static FoldReadModel Create(EventContext? context) => new();
    public static Task<FoldReadModel> Create(Placed e, int unused) => Task.FromResult(new FoldReadModel());
    public static ValueTask<FoldReadModel> Create(Derived e, int unused) => ValueTask.FromResult(new FoldReadModel());
    public static FoldReadModel Create(OrderId id) => new();
    public static FoldReadModel Create(Unmarked e) => new();
    public static FoldReadModel From(Placed e) => new();
    public static FoldReadModel Create(Placed e, int first, int second) => new();
    public static IEnumerable<FoldReadModel> Create(Placed e, string id) => [];
    [NotAQuery]
    public static FoldReadModel Helper(string id) => new();
    [NotAQuery]
    public static string OtherHelper() => string.Empty;
}

[ReadModel]
public record OverloadedReadModel(string Id)
{
    public static OverloadedReadModel Create(Placed e) => new("fold");
    public static OverloadedReadModel Create(string id) => new(id);
    [NotAQuery]
    public static OverloadedReadModel Helper() => new("hidden");
}

[ReadModel]
public record FoldOnlyReadModel
{
    public static FoldOnlyReadModel Create(Placed e) => new();
}
