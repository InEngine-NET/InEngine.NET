#!/usr/bin/env just --justfile

# The port of the local Redis server that the integration tests use.
redis_port := env("INENGINE_TEST_REDIS_PORT", "6390")

[private]
_redis_pid := justfile_directory() / "target/redis-test.pid"

# List recipes
list:
    just --list

# Default recipe
default: list

# Code quality

fmt:
    cargo fmt --all

fmt-check:
    cargo fmt --all --check

clippy:
    cargo clippy --workspace --all-targets --all-features -- -D warnings

check:
    cargo check --workspace --all-targets

machete:
    cargo machete

# Check inengine-core on its own, and fail when its package leaves out the WIT file that bindgen! reads
check-core-package:
    cargo check -p inengine-core --all-targets
    cargo package -q -p inengine-core --list --allow-dirty | grep -qx wit/plugin.wit

# Build API docs for every workspace crate, treating rustdoc warnings as errors
doc:
    RUSTDOCFLAGS="-D warnings" cargo doc --workspace --all-features --no-deps

# List files with 300 or more lines of code, excluding the test module and blank lines
list-lines:
    #!/usr/bin/env bash
    find inengine-core/src inengine/src sample-plugin/src fixture-plugin/src -name "*.rs" -not -name bindings.rs -type f | while read -r file; do
        awk -v file="$file" '
            /^[[:space:]]*#\[cfg\(test\)\][[:space:]]*$/ { held = held + 1; next }
            /^[[:space:]]*$/ { next }
            held > 0 && /^[[:space:]]*(pub(\([^)]*\))?[[:space:]]+)?mod[[:space:]]/ { exit }
            { lines = lines + held + 1; held = 0 }
            END { print lines + 0, file }
        ' "$file"
    done | sort -nr | awk '$1 >= 300'

lint: fmt clippy

# Tests

# Run unit tests
test:
    cargo nextest run --workspace

# Run unit tests with coverage, and output HTML
test-with-coverage:
    cargo llvm-cov nextest --workspace --html --ignore-filename-regex 'tests/'
    open target/llvm-cov/html/index.html

# Run unit tests with coverage, and output a summary
test-with-coverage-summary:
    cargo llvm-cov nextest --workspace --summary-only --ignore-filename-regex 'tests/'

# Start a private Redis server for the integration tests
redis-start:
    #!/usr/bin/env bash
    set -euo pipefail
    if redis-cli -p {{ redis_port }} ping > /dev/null 2>&1; then
        echo "Redis is already running on port {{ redis_port }}."
        exit 0
    fi
    mkdir -p target
    redis-server --port {{ redis_port }} --daemonize yes --save "" --appendonly no \
        --pidfile "{{ _redis_pid }}" --dir "{{ justfile_directory() }}/target"
    until redis-cli -p {{ redis_port }} ping > /dev/null 2>&1; do sleep 0.1; done
    echo "Redis is running on port {{ redis_port }}."

# Stop the Redis server that redis-start started
redis-stop:
    redis-cli -p {{ redis_port }} shutdown nosave || true

# Run unit and integration tests, including the Redis queue tests
integration-test *ARGS: redis-start
    INENGINE_TEST_REDIS_PORT={{ redis_port }} cargo nextest run --workspace --features inengine-core/integration {{ ARGS }}

# Plugins

# Build the sample plugin and copy it to target/plugins/sample/
build-sample-plugin:
    cd sample-plugin && cargo component build --release --target wasm32-unknown-unknown
    mkdir -p target/plugins/sample
    cp sample-plugin/target/wasm32-unknown-unknown/release/sample_plugin.wasm target/plugins/sample/

_fixture_dir := "inengine-core/tests/fixtures"
_fixture_wasm := "fixture-plugin/target/wasm32-unknown-unknown/release/fixture_plugin.wasm"

# Rebuild the plugin fixtures that the inengine-core tests load
build-fixture-plugin:
    cd fixture-plugin && cargo component build --release --target wasm32-unknown-unknown --features spin-on-load
    cp {{ _fixture_wasm }} {{ _fixture_dir }}/spin-on-load/fixture_plugin_spin_on_load.wasm
    cd fixture-plugin && cargo component build --release --target wasm32-unknown-unknown
    cp {{ _fixture_wasm }} {{ _fixture_dir }}/plugin/fixture_plugin.wasm

# Fail when a checked-in plugin fixture differs from a fresh build of its source
check-fixture-plugin:
    #!/usr/bin/env bash
    set -euo pipefail
    target_dir=$(mktemp -d)
    trap 'rm -rf "$target_dir"' EXIT
    built="$target_dir/wasm32-unknown-unknown/release/fixture_plugin.wasm"
    cd fixture-plugin
    cargo component build -q --release --target wasm32-unknown-unknown --target-dir "$target_dir" --features spin-on-load
    cmp "$built" ../{{ _fixture_dir }}/spin-on-load/fixture_plugin_spin_on_load.wasm
    cargo component build -q --release --target wasm32-unknown-unknown --target-dir "$target_dir"
    cmp "$built" ../{{ _fixture_dir }}/plugin/fixture_plugin.wasm
    echo "The plugin fixtures match their source."

# Run

# Run the CLI with ARGS, for example: just run queue length
run *ARGS:
    cargo run -q -p inengine -- {{ ARGS }}

# Run the server with the configuration in ./inengine.toml
server:
    cargo run -p inengine -- --server

build-release:
    cargo build --release -p inengine

# Tools

# Install the cargo tools and targets that these recipes use
install-tools:
    cargo install cargo-nextest cargo-llvm-cov cargo-machete cargo-component --locked
    rustup target add wasm32-unknown-unknown

# Holistically validate: unused dependencies, lint, docs, the inengine-core package, all tests, the sample plugin, and the plugin fixtures
validate: machete lint doc check-core-package integration-test build-sample-plugin check-fixture-plugin
    @echo "All good."
