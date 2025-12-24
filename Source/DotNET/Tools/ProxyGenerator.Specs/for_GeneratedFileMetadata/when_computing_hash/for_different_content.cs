// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileMetadata.when_computing_hash;

public class for_different_content : Specification
{
    string _hash1;
    string _hash2;

    void Because()
    {
        _hash1 = GeneratedFileMetadata.ComputeHash("export class MyClass { name: string; }");
        _hash2 = GeneratedFileMetadata.ComputeHash("export class MyClass { name: number; }");
    }

    [Fact] void should_return_different_hashes() => _hash1.ShouldNotEqual(_hash2);
}
