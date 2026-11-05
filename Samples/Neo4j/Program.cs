// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j());

var app = builder.Build();
app.UseCratisArc();
await app.RunAsync();
