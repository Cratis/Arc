// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Availability, AvailabilityPart, partNameOf } from '../../Availability.js';
import { PartIdentity } from '../../../reflection/PartIdentity.js';

describe('when asking whether a part is available and the proxy carries its identity', () => {
    const availability = new Availability([{ name: 'Fleet.Billing.Invoice', module: 'Fleet', feature: 'Billing', slice: 'Invoices' }]);
    class ListedProxy {
        static readonly part = new PartIdentity('Fleet.Billing.Invoice', 'Fleet', 'Billing', 'Invoices');
    }
    class ProxyWithAPartProperty {
        static readonly part = new PartIdentity('Fleet.Billing.Other', 'Fleet', 'Billing', 'Other');
        part = 'a value of the command';
    }

    it('should report a proxy class that is listed by name as not available', () => availability.isAvailable(ListedProxy).should.be.false);
    it('should report an instance of it as not available', () => availability.isAvailable(new ListedProxy()).should.be.false);
    it('should report the identity itself as not available', () => availability.isAvailable(ListedProxy.part).should.be.false);
    it('should not take a property of an instance for the identity', () => availability.isAvailable(new ProxyWithAPartProperty() as unknown as AvailabilityPart).should.be.true);
    it('should give the full name of a proxy', () => partNameOf(ListedProxy)!.should.equal('Fleet.Billing.Invoice'));
    it('should give the full name of an instance', () => partNameOf(new ListedProxy())!.should.equal('Fleet.Billing.Invoice'));
    it('should give no name for a proxy that carries none', () => (partNameOf({ sliceLocation: { module: 'a', feature: 'b', slice: 'c' } }) === undefined).should.be.true);
});
