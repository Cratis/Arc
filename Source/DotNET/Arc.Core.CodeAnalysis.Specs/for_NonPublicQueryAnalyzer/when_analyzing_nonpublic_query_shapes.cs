// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.NonPublicQueryAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_NonPublicQueryAnalyzer;

public class when_analyzing_nonpublic_query_shapes : given.read_model_source
{
    const string Methods = """
        internal static Order {|#0:Internal|}() => new();
        private static Task<Order?> {|#1:Private|}() => Task.FromResult<Order?>(null);
        protected static IEnumerable<Order> {|#2:Protected|}() => [];
        protected internal static ISubject<IEnumerable<Order>> {|#3:ProtectedInternal|}() => null!;
        private protected static IAsyncEnumerable<Order> {|#4:PrivateProtected|}() => null!;
        private static Order[] {|#5:Array|}() => [];
        private static Task<List<Order>> {|#6:TaskCollection|}() => Task.FromResult(new List<Order>());
        private static Task<ISubject<Order>> {|#7:TaskSubject|}() => null!;
        """;

    [Fact]
    Task should_warn_for_every_visibility_and_supported_return_shape() => VerifyCS.VerifyAnalyzerAsync(Source(Methods),
        Warning("Internal"), Warning("Private"), Warning("Protected"), Warning("ProtectedInternal"), Warning("PrivateProtected"), Warning("Array"), Warning("TaskCollection"), Warning("TaskSubject"));

    [Fact]
    Task should_also_warn_in_explicit_selection_mode() => VerifyCS.VerifyAnalyzerAsync(Source(Methods, true),
        Warning("Internal"), Warning("Private"), Warning("Protected"), Warning("ProtectedInternal"), Warning("PrivateProtected"), Warning("Array"), Warning("TaskCollection"), Warning("TaskSubject"));
}
