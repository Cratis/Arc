// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies;

public class and_the_input_is_invalid : given.a_generated_output
{
    [Theory]
    [InlineData("missing")]
    [InlineData("unloadable")]
    [InlineData("unsafe-root")]
    async Task should_fail_without_modifying_any_files(string input)
    {
        if (input != "unsafe-root") AssemblyFile = Path.Combine(Folder, "invalid.dll");
        if (input == "unloadable") File.WriteAllText(AssemblyFile, "not an assembly");
        var options = input == "unsafe-root" ? new[] { "--namespace-root=Cratis=../../escaped" } : [];
        await RunCheck(options);
        Result.ExitCode.ShouldEqual(1);
        Result.Error.ShouldContain("error:");
        VerifyUnchanged();
    }
}
