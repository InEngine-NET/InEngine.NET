# Sample Plugin

This crate is an example InEngine plugin.
A plugin is a WebAssembly component that implements the WIT interface in `rust/inengine-core/wit/plugin.wit` in the InEngine repository.
InEngine loads the component with Wasmtime and runs its commands from the command line, the queue, or a schedule.

## Prerequisites

You need `cargo-component` and the `wasm32-unknown-unknown` target:

```bash
cargo install cargo-component --locked
rustup target add wasm32-unknown-unknown
```

From the `rust` directory, `just install-tools` installs both.

## Building

From the `rust` directory, build the plugin and copy it to `target/plugins/sample/`:

```bash
just build-sample-plugin
```

You can also build it from the `sample-plugin` directory:

```bash
cargo component build --release --target wasm32-unknown-unknown
```

The component is written to `target/wasm32-unknown-unknown/release/sample_plugin.wasm`.

Build for `wasm32-unknown-unknown` rather than a WASI target.
The host provides no WASI imports, so a component built for `wasm32-wasip1` or `wasm32-wasip2` fails to load with an error about a missing `wasi:cli` implementation.

This crate reads the WIT file from `../inengine-core/wit` through the `path` option of `wit_bindgen::generate!`.
Because the crate has no `wit` folder, cargo-component does not write `src/bindings.rs`.

## Running a command

Add the directory that holds the `.wasm` file to the `[plugins]` table in `inengine.toml`.
The key is the plugin name, and the server loads every `.wasm` file in the directory:

```toml
[plugins]
sample = "target/plugins/sample"
```

A relative path resolves against the working directory.

Pass the plugin name and the command name to `inengine plugin`.
Each `--args` value is a `key=value` pair, and the plugin receives the pairs as one JSON object.
For example:

```bash
inengine plugin --name sample --command greet --args name=Ethan
```

This prints `Hello, Ethan!`.

Queue the same command with the plugin name and the command name:

```bash
inengine queue publish --plugin sample --command greet --args name=Ethan
inengine queue consume
```

## Commands

| Command   | Arguments                                | Output                  |
|-----------|------------------------------------------|-------------------------|
| `greet`   | `name`, optional, defaults to `world`    | `Hello, <name>!`        |
| `version` | none                                     | `sample-plugin 0.1.0`   |

The plugin also implements `list-schedules`, and it returns an empty list.
The server runs each schedule entry that a plugin returns.
`inengine schedule list` shows these entries.

## Writing your own plugin

1. Create a Rust library crate with `crate-type = ["cdylib"]`.
2. Add `wit-bindgen` as a dependency, and add `serde_json` if your commands read arguments.
3. Copy `rust/inengine-core/wit/plugin.wit` from the InEngine repository into a `wit` folder in your crate.
   cargo-component then writes `src/bindings.rs` on each build.
   Your crate does not use that file, because `wit_bindgen::generate!` generates the bindings, so add it to `.gitignore`.
4. Call `wit_bindgen::generate!` with `world: "plugin"`, and pass your type to `export!`.
5. Implement `exports::inengine::plugin::commands::Guest` and `exports::inengine::plugin::scheduling::Guest`.
6. Build with `cargo component build --release --target wasm32-unknown-unknown`.

`run-command` receives the command name and its arguments as a JSON string.
Return `Ok` with the text that InEngine prints, or `Err` with a message that InEngine reports as a command failure.
