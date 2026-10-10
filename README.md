# InEngine

[![CI](https://github.com/InEngine-NET/InEngine.NET/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/InEngine-NET/InEngine.NET/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/InEngine-NET/InEngine.NET/badges/tests.json)](https://github.com/InEngine-NET/InEngine.NET/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/InEngine-NET/InEngine.NET/badges/coverage.json)](https://github.com/InEngine-NET/InEngine.NET/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

A plugin-based, distributed command queuing and scheduling server.

InEngine runs commands directly, publishes them to a queue for a server to consume, or runs them on a cron schedule.
Commands come from built-in commands, from external programs that you allow in the configuration, and from WebAssembly plugins.
The queue stores messages in Redis or in files on disk.

## Usage

```shell
A command queuing and scheduling server

Usage: inengine [OPTIONS] [COMMAND]

Commands:
  echo      
  exec      
  queue     
  schedule  
  plugin    
  help      Print this message or the help of the given subcommand(s)

Options:
  -s, --server                  Run InEngine server
  -c, --configuration <CONFIG>  Path to the configuration file [default: ./inengine.toml]
  -h, --help                    Print help
```

For example, publish a command to the queue and then start the server to consume it:

```shell
inengine queue publish --plugin inengine-core --command echo --args "text=Hello, world"
inengine -s
```

InEngine reads its settings from `inengine.toml` in the working directory.
Copy `inengine.example.toml` to get started, because it lists every setting at its default value.

## Documentation

The guide in `docs` covers commands, configuration, queuing, scheduling, the server, and plugins.
If you want to read it in a browser, run `mdbook serve docs` and open the address it prints.

The sample plugin in `sample-plugin` shows how to write a plugin, and its README walks through the steps.

## Build

You need a stable Rust toolchain and [just](https://github.com/casey/just).
To build a release binary at `target/release/inengine`, run:

```shell
just build-release
```

Each CI run on `main` also builds static Linux binaries for `x86_64-unknown-linux-musl` and `aarch64-unknown-linux-musl`.
You can download them from the artifacts of the run.

## Develop

Run `just` to list the recipes.
Before your first build, run `just install-tools` to install the cargo tools and the WebAssembly target that the recipes use.

The integration tests need a Redis server.
`just integration-test` starts a private one on port 6390 with `redis-server`, so Redis must be installed.
If you already run Redis on another port, set `INENGINE_TEST_REDIS_PORT` and run `just integration-test-only`.

Before you open a pull request, run:

```shell
just validate
```

It checks formatting, lints, docs, unused dependencies, and the dependency policy in `deny.toml`.
It also runs every test, builds the sample plugin, and checks that the plugin fixtures in `inengine-core/tests/fixtures` match their source.
If you change `fixture-plugin`, run `just build-fixture-plugin` to rebuild the fixtures.

CI runs the same recipes on each pull request and once a week.

## License

InEngine is licensed under the [Apache License, Version 2.0](LICENSE).
