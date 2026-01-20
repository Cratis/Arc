// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_resolving_static_file_path;

public class with_relative_path_in_temp_directory : given.a_running_endpoint_mapper
{
    string _originalDirectory;
    string _testContent;
    HttpResponseMessage _response;
    string _responseContent;

    void Establish()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var wwwrootPath = Path.Combine(_testDirectory, "wwwroot");
        Directory.CreateDirectory(wwwrootPath);

        _testContent = "<html>Relative path test</html>";
        File.WriteAllText(Path.Combine(wwwrootPath, "index.html"), _testContent);

        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = "wwwroot" });
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/");
        _responseContent = await _response.Content.ReadAsStringAsync();
    }

    void Destroy()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
    }

    [Fact] void should_return_success() => _response.IsSuccessStatusCode.ShouldBeTrue();
    [Fact] void should_serve_file_content() => _responseContent.ShouldEqual(_testContent);
}
