// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Resources;
using Xunit;

namespace Company.XunitApplication;

public class TestingPlatformProof
{
    [Fact]
    public void ShouldIncludeBothTestingPlatformGeneratedSources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Assert.Same(assembly, typeof(SelfRegisteredExtensions).Assembly);
        Assert.Same(assembly, typeof(MicrosoftTestingPlatformApplication).Assembly);
    }

    [Fact]
    public void ShouldPreserveResxResources()
    {
        var resources = new ResourceManager("Company.XunitApplication.Messages", Assembly.GetExecutingAssembly());
        Assert.Equal("Hello from a resource", resources.GetString("Greeting", CultureInfo.InvariantCulture));
    }
}
