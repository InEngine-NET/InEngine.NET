//! Tests the errors that the command line prints.

use std::process::Command;

#[test]
fn the_cli_shows_the_cause_of_a_configuration_read_error() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let mut cli = Command::new(env!("CARGO_BIN_EXE_inengine"));
    cli.arg("-c").arg(dir.path()).args(["echo", "--text", "hi"]);

    // Act
    let output = cli.output().expect("run the CLI");

    // Assert
    let printed = String::from_utf8_lossy(&output.stderr);
    let first = format!(
        "Error: could not read the configuration file {}\n",
        dir.path().display()
    );
    assert!(!output.status.success());
    assert!(printed.starts_with(&first), "{printed}");
    assert!(printed.contains("  caused by: "), "{printed}");
    assert!(printed.to_lowercase().contains("directory"), "{printed}");
}
