# Sample Plugin

This crate is an example InEngine WASM plugin.
It demonstrates how to create commands and register schedules using the WIT plugin interface.

## Prerequisites

Install the `cargo-component` tool:

```bash
cargo install cargo-component
```

Install the `wasm32-wasip2` target:

```bash
rustup target add wasm32-wasip2
```

## Building

From the `sample-plugin` directory:

```bash
cargo component build --release
```

The compiled component is written to `target/wasm32-wasip2/release/sample_plugin.wasm`.

## Deploying

1. Copy the `.wasm` file to a directory.
2. Add the directory to the `Plugins` section of `appsettings.json`:

```json
{
  "InEngine": {
    "Plugins": {
      "sample-plugin": "/path/to/plugin/directory"
    }
  }
}
```

3. Run the plugin command:

```bash
inengine plugin --name sample-plugin --command greet --args name=Ethan
```

## Commands

This plugin provides two commands:

- **greet**: Prints a greeting. Accepts an optional `name` argument (defaults to "world").
- **version**: Prints the plugin version.

## Writing Your Own Plugin

1. Create a new Rust library crate with `crate-type = ["cdylib"]`.
2. Add `wit-bindgen` and `serde_json` as dependencies.
3. Copy the `wit/plugin.wit` file into your crate.
4. Use `wit_bindgen::generate!` to generate guest bindings.
5. Implement the `commands::Guest` and `scheduling::Guest` traits.
6. Build with `cargo component build`.
