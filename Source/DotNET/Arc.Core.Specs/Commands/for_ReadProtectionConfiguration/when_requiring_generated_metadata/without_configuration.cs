// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Commands.for_ReadProtectionConfiguration.when_requiring_generated_metadata;

public class without_configuration : Specification
{
    bool _required;

    void Because() => _required = ReadProtectionConfiguration.RequireGeneratedMetadata(null);

    [Fact] void should_require_metadata_only_without_dynamic_code() => _required.ShouldEqual(!RuntimeFeature.IsDynamicCodeSupported);
}
