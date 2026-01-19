// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_MimeTypes.when_getting_mime_type;

public class for_known_extensions : Specification
{
    [Fact] void should_return_text_html_for_html() => MimeTypes.GetMimeType(".html").ShouldEqual("text/html");
    [Fact] void should_return_text_css_for_css() => MimeTypes.GetMimeType(".css").ShouldEqual("text/css");
    [Fact] void should_return_text_javascript_for_js() => MimeTypes.GetMimeType(".js").ShouldEqual("text/javascript");
    [Fact] void should_return_application_json_for_json() => MimeTypes.GetMimeType(".json").ShouldEqual("application/json");
    [Fact] void should_return_image_png_for_png() => MimeTypes.GetMimeType(".png").ShouldEqual("image/png");
    [Fact] void should_return_image_svg_for_svg() => MimeTypes.GetMimeType(".svg").ShouldEqual("image/svg+xml");
    [Fact] void should_return_font_woff2_for_woff2() => MimeTypes.GetMimeType(".woff2").ShouldEqual("font/woff2");
    [Fact] void should_return_application_wasm_for_wasm() => MimeTypes.GetMimeType(".wasm").ShouldEqual("application/wasm");
}
