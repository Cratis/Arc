// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, field } from '@cratis/fundamentals';
import { deserializeQueryModels } from '../deserializeQueryModel';

class Item {
    @field(String)
    name!: string;
}

describe('when deserializing a collection of models', () => {
    const result = deserializeQueryModels<Item>(Item as Constructor, [{ name: 'first' }, { name: 'second' }]);

    it('should produce instances of the model type', () => result.every(_ => _ instanceof Item).should.be.true);
    it('should carry the values over', () => result.map(_ => _.name).should.deep.equal(['first', 'second']));
});
