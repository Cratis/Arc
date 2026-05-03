// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when command registers to nearest scope with nested scopes', () => {
    const parentScope = new CommandScopeImplementation(() => {});
    const childScope = new CommandScopeImplementation(() => {}, undefined, parentScope);

    const childCommand = new FakeCommand(true);
    childScope.addCommand(childCommand);

    it('should register the child scope with the parent', () => parentScope['_childScopes'].includes(childScope).should.be.true);
    it('should track the command in the child scope', () => childScope['_commands'].includes(childCommand).should.be.true);
    it('should not track the command in the parent scope directly', () => parentScope['_commands'].includes(childCommand).should.be.false);
});
