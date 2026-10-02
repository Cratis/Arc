// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// The generator never sees runtime types: <c>TypeExtensions.InitializeProjectAssemblies</c> loads every project
/// assembly through a <see cref="MetadataLoadContext"/>, so this is the only world the extractor actually runs in.
/// Rules that can only be read by instantiating a validator are therefore invisible in production even though every
/// runtime-typed spec passes.
/// </summary>
public class when_extracting_rules_from_a_metadata_only_assembly : Specification
{
    MetadataLoadContext _context;
    IEnumerable<PropertyValidationDescriptor> _fromCommandValidator;
    IEnumerable<PropertyValidationDescriptor> _fromConceptValidator;
    IEnumerable<PropertyValidationDescriptor> _fromNullableConcepts;
    IEnumerable<PropertyValidationDescriptor> _fromConstructorBoundConcepts;
    PropertyInfo _nullableGetterOnlyProperty;
    QueryDescriptor _fromNullableQuery;
    QueryDescriptor _fromNullableQueryWithArguments;
    QueryDescriptor _fromNullableControllerQuery;

    void Establish()
    {
        // Mirrors TypeExtensions.InitializeProjectAssemblies so the spec resolves exactly what the generator does.
        var assemblyFile = typeof(TestCommand).Assembly.Location;
        var runtimeDirectory = Path.GetDirectoryName(RuntimeEnvironment.GetRuntimeDirectory())!;
        var version = Path.GetFileName(runtimeDirectory);
        var shared = Directory.GetParent(Directory.GetParent(runtimeDirectory)!.FullName)!;
        var aspNetCoreDirectory = Path.Combine(shared.FullName, "Microsoft.AspNetCore.App", version);

        string[] paths =
        [
            .. Directory.GetFiles(runtimeDirectory, "*.dll"),
            .. Directory.GetFiles(aspNetCoreDirectory, "*.dll"),
            .. Directory.GetFiles(Path.GetDirectoryName(assemblyFile)!, "*.dll")
        ];

        _context = new MetadataLoadContext(new PathAssemblyResolver(paths.Distinct(new FileNameComparer())));
    }

    void Because()
    {
        var assembly = _context.LoadFromAssemblyPath(typeof(TestCommand).Assembly.Location);
        _fromCommandValidator = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(TestCommand).FullName!)!);
        _fromConceptValidator = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(TestCommandWithConcept).FullName!)!);
        _fromNullableConcepts = ValidationRulesExtractor.ExtractValidationRules(
            assembly,
            assembly.GetType(typeof(TestCommandWithNullableConcepts).FullName!)!);
        var constructorBoundType = assembly.GetType(typeof(TestCommandWithConstructorBoundConcepts).FullName!)!;
        _nullableGetterOnlyProperty = constructorBoundType.GetProperty(nameof(TestCommandWithConstructorBoundConcepts.Optional))!;
        _fromConstructorBoundConcepts = ValidationRulesExtractor.ExtractValidationRules(assembly, constructorBoundType);
        var readModel = assembly.GetType(typeof(ModelBound.for_QueryExtensions.ReadModelWithNullableConcept).FullName!)!.GetTypeInfo();
        _fromNullableQuery = readModel.ToQueryDescriptors("/output", 5, true, "api", [readModel]).Single();
        var readModelWithArguments = assembly.GetType(typeof(ModelBound.for_QueryExtensions.ReadModelWithNullableConceptAndParameters).FullName!)!.GetTypeInfo();
        _fromNullableQueryWithArguments = readModelWithArguments.ToQueryDescriptors("/output", 5, true, "api", [readModelWithArguments]).Single();
        var controller = assembly.GetType(typeof(ControllerBased.for_QueryExtensions.NullableConceptTestController).FullName!)!;
        _fromNullableControllerQuery = ControllerBased.QueryExtensions.ToQueryDescriptor(
            controller.GetMethod(nameof(ControllerBased.for_QueryExtensions.NullableConceptTestController.Find))!, "/output", 5);
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_extract_the_rules_declared_on_the_command_validator() => _fromCommandValidator.Select(_ => _.PropertyName).ShouldContain("name");
    [Fact] void should_extract_the_rules_contributed_by_a_concept_validator() => _fromConceptValidator.Select(_ => _.PropertyName).ShouldContain("email");
    [Fact] void should_skip_nullable_concept_properties() => _fromNullableConcepts.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_explicit_rules_for_nullable_concept_properties() => _fromNullableConcepts.Single(_ => _.PropertyName == "explicit").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull");
    [Fact] void should_keep_null_tolerant_rules_for_nullable_concept_properties() => _fromNullableConcepts.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_recognize_the_metadata_only_nullable_getter_only_property_as_optional() => _nullableGetterOnlyProperty.IsOptional().ShouldBeTrue();
    [Fact] void should_skip_presence_rules_for_nullable_getter_only_properties() => _fromConstructorBoundConcepts.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_presence_rules_for_required_getter_only_properties() => _fromConstructorBoundConcepts.Single(_ => _.PropertyName == "required").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_null_tolerant_rules_for_nullable_getter_only_properties() => _fromConstructorBoundConcepts.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_skip_presence_rules_for_query_parameters_with_defaults() => _fromNullableQuery.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("defaulted");
    [Fact] void should_emit_query_parameters_with_defaults_as_optional() => _fromNullableQuery.Parameters.Single(_ => _.Name == "defaulted").IsOptional.ShouldBeTrue();
    [Fact] void should_keep_null_tolerant_concept_query_rules() => _fromNullableQuery.ValidationRules.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_skip_nullable_concept_query_parameters() => _fromNullableQuery.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_non_nullable_concept_query_parameters() => _fromNullableQuery.ValidationRules.Single(_ => _.PropertyName == "required").Rules.Single().RuleName.ShouldEqual("notEmpty");
    [Fact] void should_skip_conditional_concept_query_rules() => _fromNullableQuery.ValidationRules.Single(_ => _.PropertyName == "conditional").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength", "minLength");
    [Fact] void should_omit_inferred_presence_rules_despite_non_nullable_argument_model_properties() => _fromNullableQueryWithArguments.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_preserve_explicit_argument_model_rules_alongside_null_tolerant_inferred_rules() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "explicitName").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull", "maxLength");
    [Fact] void should_preserve_null_tolerant_rules_despite_non_nullable_argument_model_properties() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_preserve_presence_rules_for_required_parameters_despite_nullable_argument_model_properties() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "required").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_preserve_argument_model_annotations() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "annotated").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_argument_model_annotations_below_non_nullable_concept_rules() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "maxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_argument_model_annotations_below_nullable_concept_rules() => _fromNullableQueryWithArguments.ValidationRules.Single(_ => _.PropertyName == "nullableMaxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_omit_presence_rules_for_nullable_controller_parameters_despite_non_nullable_dto_properties() => _fromNullableControllerQuery.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("controllerOptional");
    [Fact] void should_keep_presence_rules_for_required_controller_parameters_despite_nullable_dto_properties() => _fromNullableControllerQuery.ValidationRules.Single(_ => _.PropertyName == "controllerRequired").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_null_tolerant_rules_for_nullable_controller_parameters() => _fromNullableControllerQuery.ValidationRules.Single(_ => _.PropertyName == "controllerLimited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_preserve_explicit_controller_rules() => _fromNullableControllerQuery.ValidationRules.Single(_ => _.PropertyName == "controllerExplicit").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull", "notEmpty", "maxLength");
    [Fact] void should_keep_controller_dto_annotations_below_non_nullable_concept_rules() => _fromNullableControllerQuery.ValidationRules.Single(_ => _.PropertyName == "controllerMaxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_controller_dto_annotations_below_nullable_concept_rules() => _fromNullableControllerQuery.ValidationRules.Single(_ => _.PropertyName == "controllerNullableMaxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
}
