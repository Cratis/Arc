// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // Related query-selection fixtures.
#pragma warning disable SA1649 // Related query-selection fixtures.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

[ReadModel(RequireQueryAttribute = true)]
public partial record SelectedReadModel(string Id)
{
    public static SelectedReadModel ById(int id) => new("helper");
    public static SelectedReadModel Create(object value) => new("helper");
    public static SelectedReadModel? Reduce(object value, SelectedReadModel? current) => current;
    public static Task<SelectedReadModel> AwaitedHelper() => Task.FromResult(new SelectedReadModel("helper"));
    public static string TextHelper() => "helper";
    public static bool BoolHelper() => false;
    public SelectedReadModel InstanceHelper() => this;
    internal static SelectedReadModel InternalHelper() => new("helper");
    private static SelectedReadModel PrivateHelper() => new("helper");
    public static SelectedReadModel GenericHelper<T>(T value) => new("helper");
    public static Task BeginReplay(object service) => Task.CompletedTask;
    public static Task EndReplay(object service) => Task.CompletedTask;
    public static Task BeginReplayPartition(string partition, object service) => Task.CompletedTask;
    public static Task EndReplayPartition(string partition, object service) => Task.CompletedTask;
}

[ReadModel(RequireQueryAttribute = true)]
public record NoQueries
{
    public static NoQueries Create(object value) => new();
}

[ReadModel]
public partial record ConventionalReadModel
{
    [Query]
    public static ConventionalReadModel Marked() => new();
    public static ConventionalReadModel Create(object value) => new();
}

public partial record ConventionalReadModel
{
    public static ConventionalReadModel Sibling() => new();
}

[ReadModel(RequireQueryAttribute = true)]
public record SelectedBase;
public record InheritedSelection : SelectedBase
{
    public static InheritedSelection Helper() => new();
    [Query]
    public static InheritedSelection All() => new();
}

[ReadModel]
public record ResetSelection : SelectedBase
{
    public static ResetSelection All() => new();
}

[ReadModel(RequireQueryAttribute = true)]
public record SelectionContainer
{
    [ReadModel]
    public record Nested
    {
        public static Nested All() => new();
    }
}
