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

class PolicyValidator extends CommandValidator<{ information: string; error: string; dynamic: string }> {
    constructor() {
        super();
        this.ruleFor(c => c.information).notEmpty().withSeverity(ValidationResultSeverity.Information);
        this.ruleFor(c => c.error).notEmpty().withSeverity(ValidationResultSeverity.Error);
        this.ruleFor(c => c.dynamic).notEmpty().withSeverity(null);
    }
}

class PolicyCommand extends Command<{ information: string; error: string; dynamic: string }, void> {
    readonly route = '/test';
    readonly validation = new PolicyValidator();
    readonly blockOnValidationSeverity = ValidationResultSeverity.Warning;
    readonly propertyDescriptors: PropertyDescriptor[] = [];
    information = '';
    error = 'valid';
    dynamic = '';
    get requestParameters(): string[] { return []; }
}

describe('when executing with rule severities and a declared warning policy', () => {
    let command: PolicyCommand;
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    beforeEach(() => {
        command = new PolicyCommand(Object, false);
        command.setOrigin('http://localhost');
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({ json: async () => ({ ...CommandResult.empty, correlationId: CommandResult.empty.correlationId.toString() }) } as Response);
    });

    afterEach(() => fetchHelper.restore());

    it('lets information and runtime-dependent severity reach the server', async () => {
        const result = await command.execute();
        result.isSuccess.should.be.true;
        fetchStub.should.have.been.calledOnce;
    });

    it('blocks errors locally even with a permissive caller threshold', async () => {
        command.error = '';
        const result = await command.execute(ValidationResultSeverity.Error);
        result.isSuccess.should.be.false;
        result.validationResults.should.have.length(1);
        result.validationResults[0].severity.should.equal(ValidationResultSeverity.Error);
        fetchStub.should.not.have.been.called;
    });
});
