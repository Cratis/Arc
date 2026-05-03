// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when both parent and child scopes have their own validation failures', async () => {
    const parentFailedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [{ severity: 0, message: 'Parent field required', members: ['parentField'], state: {} }],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const childFailedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000001',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [{ severity: 0, message: 'Child field required', members: ['childField'], state: {} }],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const parentScope = new CommandScopeImplementation(() => {});
    const childScope = new CommandScopeImplementation(() => {}, undefined, parentScope);

    const parentCommand = new FakeCommand(true, parentFailedResult);
    const childCommand = new FakeCommand(true, childFailedResult);
    parentScope.addCommand(parentCommand);
    childScope.addCommand(childCommand);

    await parentScope.execute();
    await childScope.execute();

    it('should report has validation failures on the parent scope', () => parentScope.hasValidationFailures.should.be.true);
    it('should report has validation failures on the child scope', () => childScope.hasValidationFailures.should.be.true);
    it('should track parent command validation failure in parent scope', () => parentScope.validationFailures.has(parentCommand).should.be.true);
    it('should track child command validation failure in child scope', () => childScope.validationFailures.has(childCommand).should.be.true);
    it('should not track child command in parent scope directly', () => parentScope.validationFailures.has(childCommand).should.be.false);
    it('should aggregate parent and child validation failures on parent scope', () => parentScope.aggregatedValidationFailures.length.should.equal(2));
    it('should only include own validation failures on child scope aggregate', () => childScope.aggregatedValidationFailures.length.should.equal(1));
});
