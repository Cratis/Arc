// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_module_initializer_throws : given.a_generated_metadata_registration
{
    TypeInitializationException _failure;
    Exception _error;
    Exception _laterError;

    void Establish()
    {
        _failure = new("<Module>", new InvalidOperationException("Registration failed"));
        _registration.Register(_entryAssembly, Modules(("Failing.Project", () => throw _failure)), []);
    }

    void Because()
    {
        _error = Catch.Exception(_registration.EnsureRegistered);
        _laterError = Catch.Exception(_registration.EnsureRegistered);
    }

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_propagate_the_failure_again_on_later_calls() => _laterError.ShouldEqual(_failure);
}
