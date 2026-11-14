// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class ProjectionCommandValidator : AbstractValidator<ProjectionCommand>
{
    public static int Constructions;

    public ProjectionCommandValidator()
    {
        Interlocked.Increment(ref Constructions);
        RuleFor(_ => _.Text).NotEmpty().WithMessage("PRIVATE_LITERAL_6512");
        RuleFor(_ => _.Number).GreaterThan(927461);
        RuleFor(_ => _.Conditional).NotEmpty().When(_ => throw new InvalidOperationException("condition must not execute"));
        RuleFor(_ => _.Async).MustAsync((_, _) => throw new InvalidOperationException("async predicate must not execute"));
        RuleFor(_ => _.Custom).Must(_ => throw new InvalidOperationException("predicate must not execute"));
        RuleFor(_ => _.Dynamic).NotEmpty()
            .WithMessage(_ => throw new InvalidOperationException("message must not execute"))
            .WithSeverity(_ => throw new InvalidOperationException("severity must not execute"));
    }
}
