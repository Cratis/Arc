// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.for_GeneratedValueParameters.given;

public class generated_parameter_declarations : Specification
{
    protected Exception _error;

    protected static ModelBoundCommandHandler HandlerFor<T>() => new(typeof(T), typeof(T).GetMethod("Handle")!);

    public sealed record Id(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Id>
    {
        static Id IGeneratable<Id>.New() => new(GenerateValue.Uuid());
    }

    public class NonGeneratable
    {
        public void Handle([GeneratedValue] Guid id) { }
    }

    public class Nullable
    {
        public void Handle([GeneratedValue] Id? id) { }
    }

    public class DuplicateNames
    {
        public void Handle([GeneratedValue("same")] Id first, [GeneratedValue("same")] Id second) { }
    }

    public class GeneratedProvide
    {
        public void Provide([GeneratedValue] Id id) { }
        public void Handle() { }
    }

    public class ExplicitImplementation
    {
        public void Handle([GeneratedValue] Id id) { }
    }
}
