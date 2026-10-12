// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// The exception that is thrown when a concept states member numbers that are not parallel to its members.
/// </summary>
/// <param name="concept">The name of the concept.</param>
/// <param name="values">The number of members.</param>
/// <param name="numbers">The number of member numbers.</param>
public class EnumNumbersDoNotMatchValues(string concept, int values, int numbers)
    : Exception($"The concept '{concept}' has {values} enumeration members but {numbers} member numbers; numbers must be empty or one per member");
