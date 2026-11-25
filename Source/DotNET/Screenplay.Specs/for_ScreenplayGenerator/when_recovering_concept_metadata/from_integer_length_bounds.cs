// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_metadata;

public class from_integer_length_bounds : a_generated_document
{
    [Theory]
    [InlineData("3")]
    [InlineData("Minimum")]
    public void should_recover_the_minimum_with_its_message(string bound)
    {
        var source = ConceptLengthSources.Concepts([("CustomerCode", $"value is null || value.Length >= {bound}", "Customer code is too short")]);
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        var rule = Assert.Single(Result.Model.Concepts.Single().Validations);
        rule.Kind.ShouldEqual(ValidationRuleKind.Min);
        Convert.ToInt32(rule.Value).ShouldEqual(3);
        rule.Message.ShouldEqual("Customer code is too short");
    }
}
