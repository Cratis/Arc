// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../../Command.js';
import { CommandResult } from '../../CommandResult.js';
import { CommandValidator } from '../../CommandValidator.js';
import { PropertyDescriptor } from '../../../reflection/PropertyDescriptor.js';
import { ValidationResultSeverity } from '../../../validation/ValidationResultSeverity.js';
import '../../../validation/RuleBuilderExtensions.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import sinon from 'sinon';

class SeverityValidator extends CommandValidator<{ warning: string; error: string; information: string }> {
    constructor() {
        super();
        this.ruleFor(c => c.warning).notEmpty().withSeverity(ValidationResultSeverity.Warning);
        this.ruleFor(c => c.error).notEmpty();
        this.ruleFor(c => c.information).notEmpty().withSeverity(ValidationResultSeverity.Information);
    }
}

class SeverityCommand extends Command<{ warning: string; error: string; information: string }> {
    readonly route = '/test';
    readonly validation = new SeverityValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    warning = '';
    error = 'valid';
    information = 'valid';
    get requestParameters(): string[] { return []; }
}

class PolicyCommand extends SeverityCommand {
    readonly blockOnValidationSeverity = ValidationResultSeverity.Warning;
}

describe('when validating command rule severities', () => {
    let command: SeverityCommand;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        command = new SeverityCommand(Object, false);
        command.setOrigin('http://localhost');
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({ json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() }) } as Response);
    });

    afterEach(() => fetchHelper.restore());

    it('returns warning-only local results as valid advisories without a policy', () => {
        const result = command.validateClientSide();
        result.isSuccess.should.be.true;
        result.isValid.should.be.true;
        result.validationResults.should.have.length(1);
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Warning);
        fetchStub.should.not.have.been.called;
    });

    it('calls server validation with a warning-only local result and returns its advisory', async () => {
        const result = await command.validate();
        result.isSuccess.should.be.true;
        result.isValid.should.be.true;
        result.validationResults.should.have.length(1);
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Warning);
        fetchStub.should.have.been.calledOnce;
        fetchStub.firstCall.args[0].toString().should.contain('/test/validate');
    });

    it('rejects an error locally in both methods without a policy', async () => {
        command.error = '';
        const localResult = command.validateClientSide();
        const preflightResult = await command.validate();
        for (const result of [localResult, preflightResult]) {
            result.isValid.should.be.false;
            result.validationResults.should.have.length(1);
            result.validationResults[0].severity.should.equal(ValidationResultSeverity.Error);
        }
        fetchStub.should.not.have.been.called;
    });

    it('rejects a warning locally in both methods when a warning policy is declared', async () => {
        command = new PolicyCommand(Object, false);
        const localResult = command.validateClientSide();
        const preflightResult = await command.validate();
        for (const result of [localResult, preflightResult]) {
            result.isValid.should.be.false;
            result.validationResults[0].severity.should.equal(ValidationResultSeverity.Warning);
        }
        fetchStub.should.not.have.been.called;
    });

    it('returns information advisories under a declared warning policy and still calls the server', async () => {
        command = new PolicyCommand(Object, false);
        command.setOrigin('http://localhost');
        command.warning = 'valid';
        command.information = '';
        const localResult = command.validateClientSide();
        const preflightResult = await command.validate();
        for (const result of [localResult, preflightResult]) {
            result.isValid.should.be.true;
            result.validationResults.should.have.length(1);
            result.validationResults[0].severity.should.equal(ValidationResultSeverity.Information);
        }
        fetchStub.should.have.been.calledOnce;
    });

    it('rejects an error locally in both methods when a warning policy is declared', async () => {
        command = new PolicyCommand(Object, false);
        command.warning = 'valid';
        command.error = '';
        const localResult = command.validateClientSide();
        const preflightResult = await command.validate();
        for (const result of [localResult, preflightResult]) {
            result.isValid.should.be.false;
            result.validationResults[0].severity.should.equal(ValidationResultSeverity.Error);
        }
        fetchStub.should.not.have.been.called;
    });
});
