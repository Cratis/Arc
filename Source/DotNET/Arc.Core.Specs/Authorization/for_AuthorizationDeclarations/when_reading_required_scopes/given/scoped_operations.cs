// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationDeclarations.when_reading_required_scopes.given;

public class scoped_operations : Specification
{
    protected AuthorizationDeclarations _declarations;

    void Establish() => _declarations = Authorization.given.verified_callers.Declarations();

    [RequiresScope("b:write")]
    [RequiresScope("a:read")]
    public class TypeWithScopes
    {
        public void Run() { }
    }

    [RequiresScope("b:write")]
    public class TypeWithOneScope
    {
        [RequiresScope("a:read")]
        [RequiresScope("b:write")]
        public void Run() { }
    }

    public class TypeWithBlankScope
    {
        [RequiresScope(" ")]
        public void Run() { }
    }

    public class TypeWithoutScopes
    {
        public void Run() { }
    }
}
