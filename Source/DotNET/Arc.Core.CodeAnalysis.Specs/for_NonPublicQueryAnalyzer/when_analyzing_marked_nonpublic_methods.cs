// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.NonPublicQueryAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_NonPublicQueryAnalyzer;

public class when_analyzing_marked_nonpublic_methods : given.read_model_source
{
    [Fact]
    Task should_warn_even_without_a_supported_return_shape() => VerifyCS.VerifyAnalyzerAsync(Source("""
        [Query] internal static string {|#0:Internal|}() => "";
        [Query] private static void {|#1:Private|}() { }
        [Query] protected static Task {|#2:Protected|}() => Task.CompletedTask;
        [Query] protected internal static int {|#3:ProtectedInternal|}() => 0;
        [Query] private protected static object {|#4:PrivateProtected|}() => new();
        """), Warning("Internal"), Warning("Private"), Warning("Protected"), Warning("ProtectedInternal"), Warning("PrivateProtected"));
}
