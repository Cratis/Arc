// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Two members declared with one value are indistinguishable by the time the compiler hands the value over, and the
/// order the members come back in follows where they were read from rather than anything about the source. Ordering
/// the candidates by name is what keeps the same compilation producing the same document, which is the whole reason
/// a generated document is worth committing.
/// </summary>
public class an_enumeration_whose_members_share_a_value : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Members.Upgrading;

        public enum Tier
        {
            Standard = 1,
            Entry = 1,
            Basic = 1,
            Premium = 2
        }

        [EventType]
        public record TierAssigned(Tier Tier);

        [Command]
        public record UpgradeMember(string MemberId)
        {
            public object Handle() => new TierAssigned(Tier.Standard);
        }
        """;

    MappingSourceModel _source;
    MappingSourceModel _again;

    void Establish()
    {
        _source = Mapped();
        _again = Mapped();
    }

    static MappingSourceModel Mapped() =>
        Analyzed.Source(Source).Slice().Commands.First().Produces.Single().Mappings.Single().Source;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_name_the_first_of_the_members_in_ordinal_order() => _source.ShouldEqual(new LiteralSource(new EnumValue("Basic")));
    [Fact] void should_name_the_same_member_every_time() => _again.ShouldEqual(_source);
}
