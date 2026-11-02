// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

public class when_an_invoker_predates_the_kind_overload : Specification
{
    LegacyInvoker _legacy;
    IEnumerable<ValidationResult> _results;

    void Establish() => _legacy = new LegacyInvoker();

    async Task Because() => _results = await ((IValidatorInvoker)_legacy).Invoke(new object(), new InlineValidator<object>(), "path", ValidationTargetKind.Command, CancellationToken.None);

    [Fact] void should_forward_to_the_original_method() => _legacy.Path.ShouldEqual("path");
    [Fact] void should_return_what_the_original_method_returned() => _results.Single().Message.ShouldEqual("legacy");

    class LegacyInvoker : IValidatorInvoker
    {
        public string Path { get; private set; }

        public Task<IEnumerable<ValidationResult>> Invoke(object instance, IValidator validator, string path, CancellationToken cancellationToken = default)
        {
            Path = path;
            return Task.FromResult<IEnumerable<ValidationResult>>([ValidationResult.Error("legacy")]);
        }
    }
}
