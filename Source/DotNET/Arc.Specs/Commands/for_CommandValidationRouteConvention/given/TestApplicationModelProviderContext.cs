// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.given;

public class TestApplicationModelProviderContext : ApplicationModelProviderContext
{
    public TestApplicationModelProviderContext(ApplicationModel result) : base(new List<TypeInfo>())
    {
        var resultProperty = typeof(ApplicationModelProviderContext).GetProperty(nameof(Result));
        var backingField = typeof(ApplicationModelProviderContext)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(f => f.Name.Contains("result", StringComparison.OrdinalIgnoreCase) || f.Name.Contains("Result"));

        backingField?.SetValue(this, result);
    }
}
