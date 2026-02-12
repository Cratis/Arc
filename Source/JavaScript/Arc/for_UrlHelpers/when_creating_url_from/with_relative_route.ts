// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { UrlHelpers } from '../../UrlHelpers';


describe("with_relative_route", () => {
    let origin: string;
    let apiBasePath: string;
    let route: string;
    let result: URL;

    beforeEach(() => {
        origin = 'https://example.com';
        apiBasePath = '/api/v1';
        route = 'users/123'; // relative route without leading slash
    
        result = UrlHelpers.createUrlFrom(origin, apiBasePath, route);
    });

    it("should create correct url with relative route", () => {
        result.href.should.equal('https://example.com/api/users/123');
    });

    it("should have correct origin", () => {
        result.origin.should.equal('https://example.com');
    });

    it("should have correct pathname", () => {
        result.pathname.should.equal('/api/users/123');
    });
});