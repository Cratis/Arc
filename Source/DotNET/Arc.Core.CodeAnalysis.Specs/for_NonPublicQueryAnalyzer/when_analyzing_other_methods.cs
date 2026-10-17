// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.NonPublicQueryAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_NonPublicQueryAnalyzer;

public class when_analyzing_other_methods : given.read_model_source
{
    [Fact]
    Task should_ignore_public_queries_and_non_query_helpers() => VerifyCS.VerifyAnalyzerAsync(Source("""
        public static Order All() => new();
        [Query] public static Order Marked() => new();
        public static string PublicText() => "";
        private static string Text() => "";
        private static void Prepare() { }
        internal static Task BeginReplay() => Task.CompletedTask;
        private static IEnumerable<string> Names() => [];
        private static ISubject<string> Messages() => null!;
        private static Task<string> LoadText() => Task.FromResult("");
        private Order Instance() => new();
        private static Order Value => new();
        """));

    [Fact]
    Task should_ignore_types_without_a_read_model_attribute() => VerifyCS.VerifyAnalyzerAsync("""
        using Cratis.Arc.Queries.ModelBound;
        public class Other
        {
            [Query] internal static Other Marked() => new();
            private static Other Helper() => new();
        }
        """);

    [Fact]
    Task should_recognize_an_inherited_read_model_attribute() => VerifyCS.VerifyAnalyzerAsync("""
        using Cratis.Arc.Queries.ModelBound;
        [ReadModel] public class Base { }
        public class Order : Base
        {
            internal static Order {|#0:Internal|}() => new();
        }
        """, Warning("Internal"));
}
