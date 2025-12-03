# Getting Started

This guide covers the installation and basic setup of the Cratis Arc proxy generator.

## Package Dependency

To enable proxy generation, add a reference to the [Cratis.Arc.ProxyGenerator.Build](https://www.nuget.org/packages/Cratis.Arc.ProxyGenerator.Build/) NuGet package to your project:

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="1.0.0" />
```

> **Important**: All projects that contain controllers, commands, or queries should reference this package, as the proxy generation runs as part of the compilation process.

## Required Configuration

Configure the proxy generator by adding MSBuild properties to your `.csproj` file:

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web</CratisProxiesOutputPath>
</PropertyGroup>
```

- `CratisProxiesOutputPath`: Specifies where the generated TypeScript files will be written. This should typically point to your frontend project directory.

## Frontend Prerequisites

The generated proxies depend on the [@cratis/arc](https://www.npmjs.com/package/@cratis/arc) NPM package. Install it in your frontend project:

```bash
npm install @cratis/arc
```

## Build Integration

The proxy generation runs automatically during the build process. Simply build your project:

```bash
dotnet build
```

The generator will:

1. Load your compiled assembly
2. Discover controllers, commands, and queries
3. Analyze parameter types and return values
4. Generate TypeScript proxies with proper typing
5. Create index files for easy importing
6. Maintain the folder structure based on namespaces

## What Gets Generated

The proxy generator creates TypeScript proxies for:

- **Commands**: Both controller-based and model-bound approaches. See [Commands documentation](../commands/index.md) for implementation details.
- **Queries**: Single model, enumerable, and observable queries. See [Queries documentation](../queries/index.md) for implementation details.
- **Types**: Complex types used as parameters or return values
- **Enums**: Enumerations referenced by commands or queries
- **Identity Details**: Types from `IProvideIdentityDetails<TDetails>` implementations

## Next Steps

- Learn about all [Configuration Options](configuration.md)
- Understand [Command Proxy Generation](commands.md)
- Understand [Query Proxy Generation](queries.md)
- Learn about [Identity Details Type Generation](identity-details.md)
