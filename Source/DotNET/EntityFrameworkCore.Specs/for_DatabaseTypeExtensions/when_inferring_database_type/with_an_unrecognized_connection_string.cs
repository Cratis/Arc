// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_DatabaseTypeExtensions.when_inferring_database_type;

public class with_an_unrecognized_connection_string : Specification
{
    const string ConnectionString = "Server-ish=value-123;Password=abc";

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ConnectionString.GetDatabaseType());

    [Fact] void should_throw_unsupported_database_type() => _exception.ShouldBeOfExactType<UnsupportedDatabaseType>();
    [Fact] void should_use_the_fixed_message() => _exception.Message.ShouldEqual(new UnsupportedDatabaseType().Message);
    [Fact] void should_omit_the_connection_string_from_the_message() => _exception.Message.ShouldNotContain(ConnectionString);
}
