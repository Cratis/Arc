// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.given;

public class date_inputs : Specification
{
    protected static readonly JsonSerializerOptions Options = new JsonSerializerOptions().ConfigureArcDefaults();

    public record OffsetInput([property: ExplicitOffset] DateTimeOffset From);
    public record UtcInput([property: ExplicitOffset] DateTime From);
    public record NullableOffsetInput([property: ExplicitOffset] DateTimeOffset? From);
    public record NullableUtcInput([property: ExplicitOffset] DateTime? From);
    public record NestedInput(IEnumerable<OffsetInput> Items);
    public record UnsupportedInput([property: ExplicitOffset] string From);
}
