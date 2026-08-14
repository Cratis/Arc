// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents a <see cref="IMemberMapConvention"/> that materializes a read model's declared non-nullable collection
/// members as empty collections rather than null.
/// </summary>
/// <remarks>
/// <para>
/// A read model that declares a child collection as a non-nullable <c>IEnumerable&lt;T&gt;</c> promises the type system
/// that the value is there. A store is free to disagree: Chronicle's read model sink writes no field at all for a child
/// collection that has never had an element, and can write an explicit null for one whose last element went away.
/// Either way the driver hands back <see langword="null"/> for a member nullable analysis has already concluded can
/// never be null, so an unguarded <c>.Any()</c> or <c>.Select()</c> throws where nothing warned it might.
/// </para>
/// <para>
/// Chronicle closes this for its own reader through a <c>JsonTypeInfo</c> modifier on the client's serializer options.
/// That fix is confined to Chronicle's serialization boundary and never reaches the MongoDB driver — which is the
/// sanctioned path for reading a read model by anything other than its key, and the one Arc's own server-side paging
/// runs on. This convention is the same guarantee, restated where the driver can honor it.
/// </para>
/// <para>
/// Only members the declaration says cannot be null are touched. A member declared <c>IEnumerable&lt;T&gt;?</c> keeps
/// the distinction between "no collection" and "an empty collection", because that model asked for it. Dictionaries are
/// left alone — an absent map is not the same defect — and <see langword="string"/> is excluded despite being an
/// <see cref="IEnumerable"/>. A member that carries its own <see cref="BsonDefaultValueAttribute"/> has stated what it
/// wants and is skipped entirely.
/// </para>
/// <para>
/// The convention is registered under <see cref="ConventionPacks.ReadModelCollectionsNeverNull"/> and scoped to
/// <c>[ReadModel]</c> types, so it can be turned off per type with
/// <c>[IgnoreConventions(ConventionPacks.ReadModelCollectionsNeverNull)]</c>.
/// </para>
/// </remarks>
public class ReadModelCollectionsNeverNullConvention : ConventionBase, IMemberMapConvention
{
    static readonly HashSet<Type> _setDefinitions =
    [
        typeof(HashSet<>),
        typeof(ISet<>),
        typeof(IReadOnlySet<>)
    ];

    static readonly HashSet<Type> _listDefinitions =
    [
        typeof(List<>),
        typeof(IList<>),
        typeof(ICollection<>),
        typeof(IEnumerable<>),
        typeof(IReadOnlyList<>),
        typeof(IReadOnlyCollection<>)
    ];

    /// <summary>
    /// Applies the convention to a member map, giving a declared non-nullable collection an empty value for both an
    /// absent element and a stored null.
    /// </summary>
    /// <param name="memberMap">The <see cref="BsonMemberMap"/> to apply to.</param>
    public void Apply(BsonMemberMap memberMap)
    {
        if (memberMap.MemberInfo.IsDefined(typeof(BsonDefaultValueAttribute), true) ||
            !IsDeclaredNonNullable(memberMap.MemberInfo) ||
            !TryGetEmptyValueFactory(memberMap.MemberType, out var emptyValueFactory))
        {
            return;
        }

        // The driver skips a member's serializer entirely when the element is absent and reaches for the default value
        // instead, so the two halves cover different documents and both are needed. A factory rather than a shared
        // instance, because the default is handed to every instance the driver materializes.
        var serializer = WrapWithNullToEmpty(memberMap.GetSerializer(), emptyValueFactory);
        memberMap.SetDefaultValue(emptyValueFactory).SetSerializer(serializer);
    }

    static IBsonSerializer WrapWithNullToEmpty(IBsonSerializer serializer, Func<object> emptyValueFactory) =>
        (IBsonSerializer)Activator.CreateInstance(
            typeof(NullToEmptyCollectionSerializer<>).MakeGenericType(serializer.ValueType),
            serializer,
            emptyValueFactory)!;

    static bool IsDeclaredNonNullable(MemberInfo member)
    {
        // NullabilityInfoContext is documented as not thread safe and class maps are built on demand from whichever
        // thread got there first, so it is created per call rather than shared.
        var nullabilityContext = new NullabilityInfoContext();
        var nullability = member switch
        {
            PropertyInfo property => nullabilityContext.Create(property),
            FieldInfo field => nullabilityContext.Create(field),
            _ => null
        };

        return nullability?.ReadState == NullabilityState.NotNull;
    }

    static bool TryGetEmptyValueFactory(Type memberType, out Func<object> emptyValueFactory)
    {
        emptyValueFactory = null!;

        // string is the classic trap here — it is an IEnumerable<char> and would otherwise be replaced with an empty
        // list the member cannot even hold.
        if (memberType == typeof(string) || !memberType.IsAssignableTo(typeof(IEnumerable)) || IsDictionary(memberType))
        {
            return false;
        }

        if (memberType.IsArray && memberType.GetArrayRank() == 1)
        {
            var elementType = memberType.GetElementType()!;
            emptyValueFactory = () => Array.CreateInstance(elementType, 0);
            return true;
        }

        if (!memberType.IsGenericType)
        {
            return false;
        }

        var definition = memberType.GetGenericTypeDefinition();
        var elementTypeArgument = memberType.GetGenericArguments()[0];

        // Deliberately an allow list rather than "anything enumerable": the empty value is assigned straight into the
        // member, so a shape we cannot construct something assignable for must be left as it is rather than guessed at.
        if (_setDefinitions.Contains(definition))
        {
            var setType = typeof(HashSet<>).MakeGenericType(elementTypeArgument);
            emptyValueFactory = () => Activator.CreateInstance(setType)!;
            return true;
        }

        if (_listDefinitions.Contains(definition))
        {
            var listType = typeof(List<>).MakeGenericType(elementTypeArgument);
            emptyValueFactory = () => Activator.CreateInstance(listType)!;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a type is a dictionary shape.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns>True if the type is a dictionary; false otherwise.</returns>
    /// <remarks>
    /// The allow lists above already leave every dictionary shape alone, so this states the invariant rather than
    /// carrying it: whatever those lists grow to hold, a map is never something this convention fills in.
    /// </remarks>
    static bool IsDictionary(Type type) =>
        type.IsAssignableTo(typeof(IDictionary)) ||
        IsDictionaryInterface(type) ||
        Array.Exists(type.GetInterfaces(), IsDictionaryInterface);

    static bool IsDictionaryInterface(Type type) =>
        type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
         type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
}
