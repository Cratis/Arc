// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../../validation/ValidationResult';
import { ValidationResultReason } from '../../validation/ValidationResultReason';
import { ValidationResultSeverity } from '../../validation/ValidationResultSeverity';
import { CommandResult } from '../CommandResult';

describe('when creating a validation failed result', () => {
    const result = CommandResult.validationFailed([
        new ValidationResult(
            ValidationResultSeverity.Error,
            'The organization number is already in use',
            ['organizationNumber'],
            {},
            ValidationResultReason.ConstraintViolation,
            'UniqueOrganizationNumber')
    ]);

    it('should keep the reason the caller gave it', () =>
        result.validationResults[0].reason.should.equal(ValidationResultReason.ConstraintViolation));

    it('should keep the name of the violated constraint', () =>
        result.validationResults[0].reasonDetail!.should.equal('UniqueOrganizationNumber'));
});
