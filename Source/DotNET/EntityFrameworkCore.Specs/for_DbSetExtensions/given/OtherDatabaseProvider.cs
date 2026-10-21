// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.given;

public class OtherDatabaseProvider : IDatabaseProvider
{
    public string Name => "Other.Provider";
    public string Version => "1.0";
    public bool IsConfigured(IDbContextOptions options) => true;
}
