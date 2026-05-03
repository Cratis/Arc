// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when child scope has validation failure and parent checks aggregate state', async () => {
    const failedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [{ severity: 0, message: 'Name is required', members: ['name'], state: {} }],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const parentScope = new CommandScopeImplementation(() => {});
    const childScope = new CommandScopeImplementation(() => {}, undefined, parentScope);

    const parentCommand = new FakeCommand(false);
    const childCommand = new FakeCommand(true, failedResult);
    parentScope.addCommand(parentCommand);
    childScope.addCommand(childCommand);

    await childScope.execute();

    it('should report has validation failures on the child scope', () => childScope.hasValidationFailures.should.be.true);
    it('should report has validation failures on the parent scope', () => parentScope.hasValidationFailures.should.be.true);
    it('should not have validation failures on the parent scope directly', () => parentScope.validationFailures.has(childCommand).should.be.false);
    it('should not have exceptions on the child scope', () => childScope.hasExceptions.should.be.false);
    it('should not have exceptions on the parent scope', () => parentScope.hasExceptions.should.be.false);
});
