# Command Proxy Generation

The proxy generator creates TypeScript command classes that provide type-safe command execution with React hook integration.

## Supported Approaches

Commands can be implemented using two approaches, both of which are supported by the proxy generator:

- **Controller-based**: Commands in ASP.NET Core controllers using `[HttpPost]` attributes
- **Model-bound**: Simplified approach where a type represents the command directly

For detailed information on implementing commands, see the [Commands documentation](../commands/index.md).

## How Commands are Discovered

### Controller-based Commands

The generator discovers controller-based commands by looking for:

- Methods marked with `[HttpPost]`
- Parameters marked with `[FromBody]`, `[FromRoute]`, or `[FromQuery]`

See [Controller-based Commands](../commands/controller-based.md) for implementation details.

### Model-bound Commands

The generator discovers model-bound commands by finding types that:

- Are decorated with the `[Command]` attribute
- Have a `Handle()` method (the command handler)

The type name becomes the command name, and all properties of the type become the command properties in the generated TypeScript.

See [Model-bound Commands](../commands/model-bound/index.md) for implementation details.

## Generated Command Structure

Generated command classes:

- Extend the `Command` base class from `@cratis/arc/commands`
- Include all properties from route parameters, query parameters, and body content
- Provide a static `use()` method for React hook integration
- Include the proper route based on the configuration

## Generated Artifacts

For each command, the generator creates:

1. **Interface**: An `ICommandName` interface with all command properties
2. **Class**: A `CommandName` class extending `Command<ICommandName>`
3. **Route**: The HTTP route derived from the controller route or model-bound configuration

## Excluding Commands from Generation

To exclude specific controller-based commands from proxy generation, mark them with the `[AspNetResult]` attribute. This is useful when you want to handle the response manually or when the command returns a non-standard result.

## Route Configuration

The generated route is affected by the `CratisProxiesSkipCommandNameInRoute` configuration option:

- When `false` (default): The command type name is included in the route
- When `true`: The command type name is excluded from the route

See [Configuration](configuration.md) for more details on route configuration options.

## Frontend Usage

The generated command proxies integrate with React through the `use()` static method, which returns:

- The command instance with all properties
- A setter function for updating command values

The command can then be executed using the `execute()` method, which returns a `CommandResult` with success/failure information and any validation errors.

For frontend usage patterns, see the [@cratis/arc documentation](https://www.npmjs.com/package/@cratis/arc).
