// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild;

public class when_consuming_an_xunit_executable : given.an_xunit_fixture_process
{
    XDocument _report;

    async Task Because()
    {
        var reportPath = Path.Combine(RepositoryRoot(), ".ai-work", "3518", $"entrypoint-fixture-{Guid.NewGuid():N}.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await RunProcess(
            $"Source/DotNET/Screenplay.Embedded.Specs/Fixtures/XunitConsumer/bin/{Configuration}/net{Environment.Version.Major}.0/EmbeddedXunitConsumer.dll",
            "-xml",
            reportPath);
        Assert.True(_exitCode == 0, _output);
        _report = XDocument.Load(reportPath);
        _report.Descendants("test").Count().ShouldEqual(4);
    }

    [Fact] void should_have_the_real_xunit_generated_entrypoint() => AssertPassed("ShouldHaveTheRealXunitGeneratedEntrypoint");
    [Fact] void should_embed_the_application_document() => AssertPassed("ShouldEmbedTheApplicationDocument");
    [Fact] void should_embed_every_catalog_document() => AssertPassed("ShouldEmbedEveryCatalogDocument");
    [Fact] void should_preserve_the_resource_pipeline() => AssertPassed("ShouldPreserveTheResourcePipeline");

    void AssertPassed(string method) => _report.Descendants("test").Single(test => (string)test.Attribute("method")! == method).Attribute("result")!.Value.ShouldEqual("Pass");
}
