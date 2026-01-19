// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_MimeTypes.when_getting_mime_type;

public class for_unknown_extension : Specification
{
    string _result;

    void Because() => _result = MimeTypes.GetMimeType(".unknown");

    [Fact] void should_return_application_octet_stream() => _result.ShouldEqual("application/octet-stream");
}
