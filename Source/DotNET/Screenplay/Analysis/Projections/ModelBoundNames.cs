// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Holds the fully qualified metadata names of the attributes a model-bound projection is declared with.
/// </summary>
public static class ModelBoundNames
{
    /// <summary>The namespace the model-bound projection attributes live in.</summary>
    public const string Namespace = "Cratis.Chronicle.Projections.ModelBound";

    /// <summary>The attribute observing an event type.</summary>
    public const string FromEvent = $"{Namespace}.FromEventAttribute`1";

    /// <summary>The attribute applying a mapping to every observed event.</summary>
    public const string FromEvery = $"{Namespace}.FromEveryAttribute";

    /// <summary>The attribute applying a mapping to every event type in the system.</summary>
    public const string FromAll = $"{Namespace}.FromAllAttribute";

    /// <summary>The attribute mapping an event property onto a read model property.</summary>
    public const string SetFrom = $"{Namespace}.SetFromAttribute`1";

    /// <summary>The attribute mapping a constant onto a read model property.</summary>
    public const string SetValue = $"{Namespace}.SetValueAttribute`1";

    /// <summary>The attribute mapping an event context value onto a read model property.</summary>
    public const string SetFromContext = $"{Namespace}.SetFromContextAttribute`1";

    /// <summary>The attribute counting occurrences into a read model property.</summary>
    public const string Count = $"{Namespace}.CountAttribute`1";

    /// <summary>The attribute incrementing a read model property.</summary>
    public const string Increment = $"{Namespace}.IncrementAttribute`1";

    /// <summary>The attribute decrementing a read model property.</summary>
    public const string Decrement = $"{Namespace}.DecrementAttribute`1";

    /// <summary>The attribute adding an event property to a read model property.</summary>
    public const string AddFrom = $"{Namespace}.AddFromAttribute`1";

    /// <summary>The attribute subtracting an event property from a read model property.</summary>
    public const string SubtractFrom = $"{Namespace}.SubtractFromAttribute`1";

    /// <summary>The attribute joining data from another event onto a read model property.</summary>
    public const string Join = $"{Namespace}.JoinAttribute`1";

    /// <summary>The attribute building a child collection from an event.</summary>
    public const string ChildrenFrom = $"{Namespace}.ChildrenFromAttribute`1";

    /// <summary>The attribute marking a read model property as a nested object.</summary>
    public const string Nested = $"{Namespace}.NestedAttribute";

    /// <summary>The attribute clearing a nested object when an event occurs.</summary>
    public const string ClearWith = $"{Namespace}.ClearWithAttribute`1";

    /// <summary>The attribute removing a read model instance when an event occurs.</summary>
    public const string RemovedWith = $"{Namespace}.RemovedWithAttribute`1";

    /// <summary>The attribute removing a joined read model instance when an event occurs.</summary>
    public const string RemovedWithJoin = $"{Namespace}.RemovedWithJoinAttribute`1";

    /// <summary>The attribute turning automatic property mapping off.</summary>
    public const string NoAutoMap = "Cratis.Chronicle.Projections.NoAutoMapAttribute";
}
