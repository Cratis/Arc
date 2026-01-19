// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_MimeTypes.when_getting_mime_type;

public class from_file_path : Specification
{
    string _result;

    void Because() => _result = MimeTypes.GetMimeTypeFromPath("/path/to/file.css");

    [Fact] void should_return_correct_mime_type() => _result.ShouldEqual("text/css");
}
