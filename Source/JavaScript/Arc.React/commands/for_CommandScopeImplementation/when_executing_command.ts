// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing command', async () => {
    let isPerformingDuringExecution = false;
    let isPerformingAfterExecution = false;
    
    const scope = new CommandScopeImplementation(
        () => {},
        (value) => { 
            if (value) {
                isPerformingDuringExecution = true;
            } else {
                isPerformingAfterExecution = !value;
            }
        }
    );

    const command = new FakeCommand(true);
    scope.addCommand(command);

    await scope.execute();

    it('should set is performing to true during execution', () => isPerformingDuringExecution.should.be.true);
    it('should set is performing to false after execution', () => isPerformingAfterExecution.should.be.true);
    it('should have is performing as false', () => scope.isPerforming.should.be.false);
});
