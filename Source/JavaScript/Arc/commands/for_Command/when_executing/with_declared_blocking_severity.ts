// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command';
import { CommandValidator } from '../../CommandValidator';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor';
import { ValidationResult } from '../../../validation/ValidationResult';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity';

class WarningValidator extends CommandValidator {
    severity = ValidationResultSeverity.Information;
    validate(): ValidationResult[] {
        return [new ValidationResult(this.severity, 'Original message', [], null)];
    }
}

class PolicyCommand extends Command {
    readonly route = '/test';
    readonly validation = new WarningValidator();
    readonly blockOnValidationSeverity = ValidationResultSeverity.Information;
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    get requestParameters(): string[] { return []; }
}

describe('when executing with declared blocking severity', () => {
    let command: PolicyCommand;
    beforeEach(() => { command = new PolicyCommand(Object, false); });

    it('blocks information locally despite a permissive caller threshold', async () => {
        const result = await command.execute(ValidationResultSeverity.Error, true);
        result.isSuccess.should.be.false;
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Information);
        result.validationResults[0].message.should.equal('Original message');
    });

    it('blocks information on preflight validation', async () => {
        const result = await command.validate();
        result.isSuccess.should.be.false;
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Information);
    });

    it('blocks an unclassified failure without changing its severity', async () => {
        command.validation.severity = ValidationResultSeverity.Unknown;
        const result = await command.execute();
        result.isSuccess.should.be.false;
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Unknown);
    });
});
