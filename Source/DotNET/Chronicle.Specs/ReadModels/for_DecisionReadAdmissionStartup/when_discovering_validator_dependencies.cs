// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Types;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_discovering_validator_dependencies : Specification
{
    (Type Model, bool StreamScoped)[] _reads;

    void Because()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(InheritedValidator), typeof(UnrelatedValidator)]);
        _reads = DeclaredDecisionReadParameters.ReadsOf(typeof(Command), new HashSet<Type> { typeof(Model), typeof(UnrelatedModel) }, types, (true, null)).ToArray();
    }

    [Fact] void should_find_the_inherited_validator_dependency() => _reads.ShouldContain((typeof(Model), false));
    [Fact] void should_ignore_other_commands_validators() => _reads.Length.ShouldEqual(1);

    public record Model;
    public record UnrelatedModel;

    [ReadProtection(ReadProtectionMode.On)]
    public class Command
    {
        public void Handle() { }
    }

    public class OtherCommand;

    public abstract class BaseValidator(Model model) : CommandValidator<Command>
    {
        public Model Model { get; } = model;
    }

    public class InheritedValidator(Model model) : BaseValidator(model);

    public class UnrelatedValidator(UnrelatedModel model) : CommandValidator<OtherCommand>
    {
        public UnrelatedModel Model { get; } = model;
    }
}
