// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_DecisionReadAdmissions;

public class when_checking_declared_read_protection : Specification
{
    IReadOnlyList<RefusedDecisionRead> _refused;
    IReadOnlyList<RefusedDecisionRead> _assemblyRefused;
    Exception? _scanFailure;
    Type _assemblyCommand;
    Type _customProtectedCommand;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AdmissionOn{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadProtectionAttribute).GetConstructor([typeof(ReadProtectionMode)])!, [ReadProtectionMode.On]));
        var module = assembly.DefineDynamicModule("Commands");
        var command = module.DefineType("AssemblyOnCommand", TypeAttributes.Public, typeof(AssemblyCommand));
        command.SetCustomAttribute(new CustomAttributeBuilder(typeof(CommandAttribute).GetConstructor(Type.EmptyTypes)!, []));
        _assemblyCommand = command.CreateType()!;
        var customProtectedCommand = module.DefineType("CustomProtectedCommand", TypeAttributes.Public, typeof(AssemblyCommand));
        customProtectedCommand.SetCustomAttribute(new CustomAttributeBuilder(typeof(CustomProtectedDecisionAttribute).GetConstructor(Type.EmptyTypes)!, []));
        _customProtectedCommand = customProtectedCommand.CreateType()!;
        module.DefineType("NotACommand", TypeAttributes.Public, typeof(AssemblyCommand)).CreateType();
    }

    void Because()
    {
        _refused = DecisionReadAdmissions.FindRefused([typeof(PlainRead), typeof(ValidatorRead), typeof(AdvisoryRead), typeof(OffRead), typeof(MeasureRead), typeof(StreamRead), typeof(RefusedStreamRead), typeof(WrappedRead), typeof(WrappedStreamRead), typeof(EnumeratedWrappedStreamRead)]);
        _assemblyRefused = DecisionReadAdmissions.FindRefused([_assemblyCommand]);
        _scanFailure = Catch.Exception(() => DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(_assemblyCommand.Assembly));
    }

    [Fact] void should_check_plain_parameters() => _refused.ShouldContain(new RefusedDecisionRead(typeof(PlainRead), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_check_discoverable_validator_constructor_parameters() => _refused.ShouldContain(new RefusedDecisionRead(typeof(ValidatorRead), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_check_wrapped_reads_without_protected_decision() => _refused.ShouldContain(new RefusedDecisionRead(typeof(WrappedRead), typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_admit_a_plain_stream_model_using_stream_admission() => _refused.Any(_ => _.CommandType == typeof(StreamRead)).ShouldBeFalse();
    [Fact] void should_admit_a_wrapped_stream_model_using_stream_admission() => _refused.Any(_ => _.CommandType == typeof(WrappedStreamRead)).ShouldBeFalse();
    [Fact] void should_admit_enumerated_wrapped_stream_models_using_stream_admission() => _refused.Any(_ => _.CommandType == typeof(EnumeratedWrappedStreamRead)).ShouldBeFalse();
    [Fact] void should_refuse_an_unsupported_stream_model() => _refused.ShouldContain(new RefusedDecisionRead(typeof(RefusedStreamRead), typeof(StreamShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_skip_advisory_off_measure_and_service_parameters() => _refused.Count.ShouldEqual(4);
    [Fact] void should_check_assembly_on_declarations() => _assemblyRefused.ShouldContain(new RefusedDecisionRead(_assemblyCommand, typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_include_assembly_on_commands_in_assembly_scans() => _scanFailure.ShouldBeOfExactType<DecisionReadsAreRefused>();
    [Fact] void should_include_custom_protected_decision_attributes_in_assembly_scans() => ((DecisionReadsAreRefused)_scanFailure!).Refusals.ShouldContain(new RefusedDecisionRead(_customProtectedCommand, typeof(AdmissionShelf), DecisionReadRefusalReason.Hierarchy));
    [Fact] void should_not_scan_services_with_handle_methods_as_commands() => ((DecisionReadsAreRefused)_scanFailure!).Refusals.Count.ShouldEqual(2);

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record PlainRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(AdmissionShelf shelf, IServiceProvider service) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record ValidatorRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle() => new();
    }

    public class ShelfValidator(AdmissionShelf shelf) : CommandValidator<ValidatorRead>
    {
        public AdmissionShelf Shelf { get; } = shelf;
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record AdvisoryRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle([Unprotected("Display only")] AdmissionShelf shelf) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.Off)]
    public record OffRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(AdmissionShelf shelf) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.Measure)]
    public record MeasureRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(AdmissionShelf shelf) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record WrappedRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(DecisionRead<AdmissionShelf> shelf) => new();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomProtectedDecisionAttribute : Attribute, IProtectedDecisionAttribute;

    public class AssemblyCommand
    {
        public AdmissionDecided Handle(AdmissionShelf shelf) => new();
    }

    [Cratis.Chronicle.EventSources.EventSource("Admission")]
    [Cratis.Chronicle.EventSources.EventStream("Shelves")]
    public class ShelfSource : IEventSource;

    [EventStream<ShelfSource>("Shelves"), FromEvent<AdmissionFlagRaised>]
    public record StreamFlag(Guid Id);

    [EventStream<ShelfSource>("Shelves"), FromEvent<AdmissionShelfInstalled>]
    public record StreamShelf(
        Guid Id,
        [ChildrenFrom<AdmissionBookShelved>(key: nameof(AdmissionBookShelved.BookId), parentKey: nameof(AdmissionBookShelved.ShelfId))]
        IEnumerable<AdmissionShelvedBook> Books);

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record StreamRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(StreamFlag flag) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record WrappedStreamRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(DecisionRead<StreamFlag> flag) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record EnumeratedWrappedStreamRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(IEnumerable<DecisionRead<StreamFlag>> flags) => new();
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record RefusedStreamRead(EventSourceId EventSourceId)
    {
        public AdmissionDecided Handle(StreamShelf shelf) => new();
    }
}
