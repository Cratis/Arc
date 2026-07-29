// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_GeneratedSource;

/// <summary>
/// Source a build wrote is told from source a person wrote by where it sits and by what it is called, because those
/// are the only two things a path says. Both halves matter - a generator emits under the intermediate folder, and a
/// designer writes a companion file next to the source it belongs to.
/// </summary>
public class when_recognizing_a_path : Specification
{
    bool _underTheIntermediateFolder;
    bool _underTheOutputFolder;
    bool _windowsSeparated;
    bool _namedAsGenerated;
    bool _writtenByAPerson;
    bool _namedAfterTheOutputFolder;
    bool _nothingAtAll;

    void Because()
    {
        _underTheIntermediateFolder = GeneratedSource.Is("Library/obj/Debug/net10.0/Some.Generator/Slice.g.cs");
        _underTheOutputFolder = GeneratedSource.Is("Library/bin/Release/net10.0/Slice.cs");
        _windowsSeparated = GeneratedSource.Is(@"C:\Work\Library\obj\Debug\Slice.cs");
        _namedAsGenerated = GeneratedSource.Is("Library/Authors/Registration/Registration.generated.cs");
        _writtenByAPerson = GeneratedSource.Is("Library/Authors/Registration/Registration.cs");
        _namedAfterTheOutputFolder = GeneratedSource.Is("Library/Authors/obj.cs");
        _nothingAtAll = GeneratedSource.Is(null);
    }

    [Fact] void should_recognize_source_under_the_intermediate_folder() => _underTheIntermediateFolder.ShouldBeTrue();
    [Fact] void should_recognize_source_under_the_output_folder() => _underTheOutputFolder.ShouldBeTrue();
    [Fact] void should_recognize_a_path_separated_the_way_windows_separates_one() => _windowsSeparated.ShouldBeTrue();
    [Fact] void should_recognize_a_file_named_as_generated() => _namedAsGenerated.ShouldBeTrue();
    [Fact] void should_leave_source_a_person_wrote_alone() => _writtenByAPerson.ShouldBeFalse();
    [Fact] void should_not_read_a_file_name_as_a_folder() => _namedAfterTheOutputFolder.ShouldBeFalse();
    [Fact] void should_answer_for_a_path_that_is_not_there() => _nothingAtAll.ShouldBeFalse();
}
