# Proxy Generation

Cratis Arc includes a powerful proxy generation tool that automatically creates TypeScript proxies for your commands and queries during the build process. This eliminates the need to manually write frontend integration code or consult Swagger documentation, as the proxies provide compile-time type safety and intellisense support.

## Overview

The proxy generator runs as part of your build process using the C# Roslyn compiler's code generator extensibility. It analyzes your compiled assemblies and generates TypeScript representations for:

- **Commands**: HTTP POST operations (both controller-based and model-bound)
- **Queries**: HTTP GET operations (single, enumerable, and observable)
- **Types**: Complex types used in command/query parameters or return values
- **Enums**: Enumerations used in your domain models
- **Identity Details**: Custom identity types from `IProvideIdentityDetails<TDetails>` implementations

## Supported Approaches

The proxy generator supports both command and query implementation patterns:

- **Controller-based**: Traditional ASP.NET Core controllers with `[HttpPost]` and `[HttpGet]` attributes
- **Model-bound**: Simplified approach where types represent the command or query directly

Both approaches generate equivalent TypeScript proxies, allowing you to choose the implementation style that best fits your needs.

## How It Works

The proxy generation runs automatically during the build process through the `CratisProxyGenerator` MSBuild target, which executes after the `AfterBuild` target. The generator:

1. Loads your compiled assembly
2. Discovers controllers, commands, and queries
3. Analyzes parameter types and return values
4. Discovers identity details types from `IProvideIdentityDetails<TDetails>` implementations
5. Generates TypeScript proxies with proper typing
6. Creates index files for easy importing
7. Maintains the folder structure based on namespaces
8. Tracks generated files for intelligent cleanup

## Generated Output Structure

The proxy generator maintains the folder structure based on your C# namespaces (after applying segment skipping). For each directory containing generated files, an `index.ts` file is automatically created that exports all the generated artifacts.

## Topics

- [Getting Started](getting-started.md) - Installation and setup
- [Configuration](configuration.md) - All configuration options
- [Commands](commands.md) - Command proxy generation
- [Queries](queries.md) - Query proxy generation
- [Identity Details](identity-details.md) - Identity details type generation
- [File Index Tracking](file-index-tracking.md) - Incremental cleanup of generated files
