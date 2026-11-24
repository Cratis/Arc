// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Base type of an availability block: a reusable rule defined by lines that must all hold.
/// </summary>
/// <remarks>
/// <para>
/// A block is a type, and its name is the name of the type. Its lines are written with operators, in the shape the
/// language has them:
/// </para>
/// <code>
/// public class DataQuality : AvailabilityBlock
/// {
///     protected override IReadOnlyList&lt;AvailabilityTerm&gt; Lines =>
///     [
///         For&lt;Plus&gt;() | For&lt;Preview&gt;(),
///         When.On&lt;Vessel&gt;(_ => _.HasReported).IsTrue()
///     ];
/// }
/// </code>
/// <para>
/// A line holds one form. 'for' and 'with' combine with <c>&amp;</c>, <c>|</c> and parentheses, <c>Not</c> applies to one
/// audience and a 'when' is one condition with its own <c>&amp;</c> and <c>|</c>. A line that mixes forms with <c>&amp;</c> is
/// the same as the forms on lines of their own.
/// </para>
/// </remarks>
public abstract class AvailabilityBlock
{
    /// <summary>
    /// Gets the way to start a 'when' condition.
    /// </summary>
    protected static AvailabilityWhen When { get; } = new();

    /// <summary>
    /// Gets the lines of the block, all of which must hold.
    /// </summary>
    protected abstract IReadOnlyList<AvailabilityTerm> Lines { get; }

    /// <summary>
    /// Gets the name of a block, which is the name of its type.
    /// </summary>
    /// <param name="block">The type of the block.</param>
    /// <returns>The name.</returns>
    public static string NameOf(Type block) => block.Name;

    /// <summary>
    /// Defines the block for the engine.
    /// </summary>
    /// <param name="conditions">Creates the condition of an <see cref="IAvailabilityCondition"/> class.</param>
    /// <returns>The definition.</returns>
    public AvailabilityDefinition Define(Func<Type, IAvailabilityCondition> conditions) =>
        AvailabilityDefinition.FromLines(NameOf(GetType()), [.. Lines.Select(_ => _.ToExpression(conditions))]);

    /// <summary>
    /// A 'for' over an audience: holds when the audience is held.
    /// </summary>
    /// <typeparam name="TAudience">The audience.</typeparam>
    /// <returns>The term.</returns>
    protected static AvailabilityTerm For<TAudience>()
        where TAudience : Audience => new AudienceTerm(typeof(TAudience), false);

    /// <summary>
    /// A 'not' over an audience: holds when the audience is not held. An unknown audience keeps the gate closed.
    /// </summary>
    /// <typeparam name="TAudience">The audience.</typeparam>
    /// <returns>The term.</returns>
    protected static AvailabilityTerm Not<TAudience>()
        where TAudience : Audience => new AudienceTerm(typeof(TAudience), true);

    /// <summary>
    /// A 'with' over another block: holds when the block holds.
    /// </summary>
    /// <typeparam name="TBlock">The block.</typeparam>
    /// <returns>The term.</returns>
    protected static AvailabilityTerm With<TBlock>()
        where TBlock : AvailabilityBlock => new BlockTerm(typeof(TBlock));
}
