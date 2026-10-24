// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild;

public class when_consuming_an_xunit_mtp_executable : given.an_xunit_fixture_process
{
    XDocument _report;

    async Task Because()
    {
        var reportDirectory = Path.Combine(RepositoryRoot(), ".ai-work", "3518", $"mtp-fixture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(reportDirectory);
        await RunProcess(
            $"Source/DotNET/Screenplay.Embedded.Specs/Fixtures/XunitMtpConsumer/bin/{Configuration}/net{Environment.Version.Major}.0/EmbeddedXunitMtpConsumer.dll",
            "--report-xunit-xml",
            "--report-xunit-xml-filename",
            "results.xml",
            "--results-directory",
            reportDirectory);
        Assert.True(_exitCode == 0, _output);
        _report = XDocument.Load(Path.Combine(reportDirectory, "results.xml"));
        _report.Descendants("test").Count().ShouldEqual(6);
    }

    [Fact] void should_have_the_real_xunit_generated_entrypoint() => AssertPassed("ShouldHaveTheRealXunitGeneratedEntrypoint");
    [Fact] void should_attach_both_testing_platform_generated_sources() => AssertPassed("ShouldIncludeBothTestingPlatformGeneratedSources");
    [Fact] void should_embed_the_application_document() => AssertPassed("ShouldEmbedTheApplicationDocument");
    [Fact] void should_embed_every_catalog_document() => AssertPassed("ShouldEmbedEveryCatalogDocument");
    [Fact] void should_preserve_the_resource_pipeline() => AssertPassed("ShouldPreserveTheResourcePipeline");
    [Fact] void should_preserve_resx_resources() => AssertPassed("ShouldPreserveResxResources");

    void AssertPassed(string method) => _report.Descendants("test").Single(test => (string)test.Attribute("method")! == method).Attribute("result")!.Value.ShouldEqual("Pass");
}
