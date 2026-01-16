// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_shadow_properties_with_concepts;

public class when_configuring_existing_property_using_shadow_property_syntax : given.a_test_database
{
    Exception? _exception;

    async Task Because()
    {
        try
        {
            // This should fail because we're trying to access the model which triggers model creation
            var model = _context.Model;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_throw_exception() => _exception.ShouldBeNull();
}
