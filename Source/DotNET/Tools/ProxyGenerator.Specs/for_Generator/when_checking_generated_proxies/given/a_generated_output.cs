// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_Generator.when_checking_generated_proxies.given;

public class a_generated_output : when_generating_incrementally.given.a_previous_generation
{
    Dictionary<string, byte[]> _bytes;
    Dictionary<string, DateTime> _times;
    string[] _directories;

    protected async Task RunCheck(params string[] arguments)
    {
        _bytes = Directory.GetFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
        _times = Directory.GetFileSystemEntries(Folder, "*", SearchOption.AllDirectories).Append(Folder).ToDictionary(path => path, File.GetLastWriteTimeUtc);
        _directories = Directory.GetDirectories(Folder, "*", SearchOption.AllDirectories);
        Result = await RunGenerator(true, ["--check", .. arguments]);
    }

    protected void VerifyUnchanged()
    {
        Directory.GetFiles(Folder, "*", SearchOption.AllDirectories).Order().ShouldEqual(_bytes.Keys.Order());
        Directory.GetDirectories(Folder, "*", SearchOption.AllDirectories).Order().ShouldEqual(_directories.Order());
        _bytes.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)).ShouldBeTrue();
        _times.All(pair => File.GetLastWriteTimeUtc(pair.Key) == pair.Value).ShouldBeTrue();
    }
}
