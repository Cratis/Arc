// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper.when_resolving_static_file_path;

public class with_absolute_path : given.a_running_endpoint_mapper
{
    string _absolutePath;
    bool _fileExists;
    string _testContent;

    void Establish()
    {
        _absolutePath = Path.Combine(_testDirectory, "absolute_files");
        Directory.CreateDirectory(_absolutePath);

        _testContent = "<html>Absolute path test</html>";
        File.WriteAllText(Path.Combine(_absolutePath, "index.html"), _testContent);

        _endpointMapper.UseStaticFiles(new StaticFileOptions { FileSystemPath = _absolutePath });
        StartEndpointMapper();
    }

    async Task Because() => _fileExists = (await _httpClient.GetAsync("/")).IsSuccessStatusCode;

    [Fact] void should_serve_files_from_absolute_path() => _fileExists.ShouldBeTrue();
}
