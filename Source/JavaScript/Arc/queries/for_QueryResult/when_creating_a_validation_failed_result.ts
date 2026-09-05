// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../../validation/ValidationResult';
import { ValidationResultReason } from '../../validation/ValidationResultReason';
import { ValidationResultSeverity } from '../../validation/ValidationResultSeverity';
import { QueryResult } from '../QueryResult';

// The query side is meant to read the same way the command side does, which only holds if it carries
// the same fields through - so this is the mirror of the CommandResult spec of the same name.
describe('when creating a validation failed result', () => {
    const result = QueryResult.validationFailed([
        new ValidationResult(
            ValidationResultSeverity.Error,
            'The organization number is already in use',
            ['organizationNumber'],
            {},
            ValidationResultReason.ConstraintViolation,
            'UniqueOrganizationNumber')
    ], { defaultValue: {}, modelType: Object, enumerable: false });

    it('should keep the reason the caller gave it', () =>
        result.validationResults[0].reason.should.equal(ValidationResultReason.ConstraintViolation));

    it('should keep the name of the violated constraint', () =>
        result.validationResults[0].reasonDetail!.should.equal('UniqueOrganizationNumber'));
});
