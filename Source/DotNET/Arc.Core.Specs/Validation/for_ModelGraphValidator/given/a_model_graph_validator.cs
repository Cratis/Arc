// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.given;

public class a_model_graph_validator : Specification
{
    protected IDiscoverableValidators _discoverableValidators;
    protected ModelGraphValidator _validator;
    protected List<Type> _typesAskedFor;

    void Establish()
    {
        _typesAskedFor = [];
        _discoverableValidators = Substitute.For<IDiscoverableValidators>();

        // Record every type the traversal asks about, which is how the specs observe where it went without
        // depending on the validator implementations themselves.
        _discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                _typesAskedFor.Add((Type)x[0]);
                return false;
            });

        _validator = new ModelGraphValidator(_discoverableValidators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
    }

    /// <summary>
    /// Registers a validator for a type so the traversal discovers it, mirroring how
    /// <see cref="IDiscoverableValidators"/> resolves a convention-discovered validator at runtime.
    /// </summary>
    /// <param name="type">The model <see cref="Type"/> the validator applies to.</param>
    /// <param name="validator">The <see cref="IValidator"/> to return for it.</param>
    protected void WithValidatorFor(Type type, IValidator validator) =>
        _discoverableValidators.TryGet(type, out Arg.Any<IValidator>())
            .Returns(x =>
            {
                _typesAskedFor.Add(type);
                x[1] = validator;
                return true;
            });

    /// <summary>
    /// Whether the traversal descended into a type, observed by whether it looked for a validator for any of that
    /// type's own member types.
    /// </summary>
    /// <param name="memberType">A <see cref="Type"/> that only appears as a member of the type in question.</param>
    /// <returns>True when the traversal reached it.</returns>
    protected bool DescendedInto(Type memberType) => _typesAskedFor.Contains(memberType);
}
