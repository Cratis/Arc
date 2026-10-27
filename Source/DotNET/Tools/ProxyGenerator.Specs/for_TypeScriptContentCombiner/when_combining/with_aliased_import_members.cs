// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_aliased_import_members : Specification
{
    const string Header = "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n";
    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([
        Header + "import { SliceLocation as ArcSliceLocation, PropertyDescriptor } from '@cratis/arc/reflection';\nexport class First {}",
        Header + "import { SliceLocation as ArcSliceLocation, ParameterDescriptor } from '@cratis/arc/reflection';\nexport class SliceLocation {}"
    ]);

    [Fact] void should_sort_members_by_their_local_name() => _result.ShouldContain("import { SliceLocation as ArcSliceLocation, ParameterDescriptor, PropertyDescriptor } from '@cratis/arc/reflection';");
    [Fact] void should_keep_the_aliased_import_when_the_original_name_is_declared_in_the_file() => _result.ShouldContain("SliceLocation as ArcSliceLocation");
}
