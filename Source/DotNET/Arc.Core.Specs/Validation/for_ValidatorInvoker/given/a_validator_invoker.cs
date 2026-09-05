// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_ValidatorInvoker.given;

public class a_validator_invoker : Specification
{
    protected ValidatorInvoker _invoker;

    void Establish() => _invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
}
