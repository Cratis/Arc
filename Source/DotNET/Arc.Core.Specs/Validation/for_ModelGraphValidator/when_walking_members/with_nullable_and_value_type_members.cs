// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// A null member is skipped, a nullable value type is walked as its value, and a struct is walked like a class.
/// </summary>
public class with_nullable_and_value_type_members : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Root(null, 5, null, new Point(1, 2), new Point(3, 4)));

    [Fact] void should_skip_nulls_and_walk_values() =>
        Traversal.ShouldEqual("Root@ | Int32@count | Point@point | Int32@point.x | Int32@point.y | Point@maybePoint | Int32@maybePoint.x | Int32@maybePoint.y");

    [StructLayout(LayoutKind.Auto)]
    record struct Point(int X, int Y);

    record Root(string? Missing, int? Count, int? NoCount, Point Point, Point? MaybePoint);
}
