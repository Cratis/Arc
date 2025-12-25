// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileMetadata.when_computing_hash;

public class for_same_content : Specification
{
    string _hash1;
    string _hash2;
    string _content;

    void Establish()
    {
        _content = "export class MyClass { name: string; }";
    }

    void Because()
    {
        _hash1 = GeneratedFileMetadata.ComputeHash(_content);
        _hash2 = GeneratedFileMetadata.ComputeHash(_content);
    }

    [Fact] void should_return_same_hash() => _hash1.ShouldEqual(_hash2);
    [Fact] void should_not_be_empty() => _hash1.ShouldNotBeEmpty();
    [Fact] void should_be_hex_string() => _hash1.All(char.IsAsciiHexDigit).ShouldBeTrue();
}
