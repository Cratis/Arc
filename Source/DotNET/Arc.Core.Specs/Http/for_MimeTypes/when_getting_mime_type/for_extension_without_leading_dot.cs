// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_MimeTypes.when_getting_mime_type;

public class for_extension_without_leading_dot : Specification
{
    string _result;

    void Because() => _result = MimeTypes.GetMimeType("html");

    [Fact] void should_return_correct_mime_type() => _result.ShouldEqual("text/html");
}
