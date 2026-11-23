// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

public static class BranchingUnionSource
{
    public static string Declarations(string leaf, bool tuples = false) => tuples
        ? "public union Outcome(Nested, Alternative, " + leaf + "); public union Nested((int, Outcome), Alternative); public union Alternative(Outcome, (int, Nested));"
        : "public union Outcome(Nested, Alternative, " + leaf + "); public union Nested(Outcome, Alternative); public union Alternative(Outcome, Nested);";
}
#endif
