use std::process::{Command, Stdio};
use std::time::Duration;

#[test]
fn log_lines_written_to_a_pipe_have_no_color_codes() {
    // Arrange
    let dir = tempfile::tempdir().expect("temporary directory");
    let config = dir.path().join("inengine.toml");
    std::fs::write(
        &config,
        "[queue]\nqueue_driver = \"redis\"\n[queue.redis]\nport = 1\n",
    )
    .expect("write configuration");
    let mut server = Command::new(env!("CARGO_BIN_EXE_inengine"))
        .args(["-c", config.to_str().expect("utf-8 path"), "-s"])
        .env("INENGINE_LOG", "info")
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .expect("start the server");
    std::thread::sleep(Duration::from_millis(500));
    server.kill().expect("stop the server");

    // Act
    let output = server.wait_with_output().expect("server output");

    // Assert
    let logged = String::from_utf8_lossy(&output.stdout);
    assert!(logged.contains("starting server"), "{logged}");
    assert!(!logged.contains('\u{1b}'), "{logged:?}");
}
