use std::io::{BufRead, BufReader, Write};
use std::net::{TcpListener, TcpStream};
use std::sync::mpsc;
use std::time::Duration;

const RECEIVE_LIMIT: Duration = Duration::from_secs(5);

/// One message as the server received it.
#[derive(Debug)]
pub struct ReceivedMail {
    /// Every command of the session before the message text, such as `EHLO` and `MAIL FROM`.
    pub commands: Vec<String>,
    pub text: String,
}

/// An SMTP server that offers no extensions, so no STARTTLS and no AUTH, and accepts every
/// message. Its thread runs until the test process ends.
pub struct PlainSmtpServer {
    pub port: u16,
    receiver: mpsc::Receiver<ReceivedMail>,
}

impl PlainSmtpServer {
    pub fn start() -> Self {
        let listener = TcpListener::bind("127.0.0.1:0").expect("bind the SMTP server");
        let port = listener.local_addr().expect("address").port();
        let (sender, receiver) = mpsc::channel();
        std::thread::spawn(move || {
            for stream in listener.incoming() {
                let Ok(stream) = stream else { return };
                let _ = serve_session(stream, &sender);
            }
        });
        Self { port, receiver }
    }

    /// The next message, or `None` when none arrives within five seconds.
    pub fn next_mail(&self) -> Option<ReceivedMail> {
        self.receiver.recv_timeout(RECEIVE_LIMIT).ok()
    }
}

fn serve_session(stream: TcpStream, sender: &mpsc::Sender<ReceivedMail>) -> std::io::Result<()> {
    let mut writer = stream.try_clone()?;
    let mut reader = BufReader::new(stream);
    writer.write_all(b"220 localhost\r\n")?;
    let mut commands = Vec::new();
    loop {
        let mut line = String::new();
        if reader.read_line(&mut line)? == 0 {
            return Ok(());
        }
        let command = line.trim_end().to_string();
        let verb = command
            .split_whitespace()
            .next()
            .unwrap_or_default()
            .to_ascii_uppercase();
        commands.push(command);
        match verb.as_str() {
            "EHLO" => writer.write_all(b"250 localhost\r\n")?,
            "DATA" => {
                writer.write_all(b"354 send the message\r\n")?;
                let text = read_message_text(&mut reader)?;
                writer.write_all(b"250 accepted\r\n")?;
                let _ = sender.send(ReceivedMail {
                    commands: std::mem::take(&mut commands),
                    text,
                });
            }
            "QUIT" => {
                writer.write_all(b"221 bye\r\n")?;
                return Ok(());
            }
            _ => writer.write_all(b"250 ok\r\n")?,
        }
    }
}

fn read_message_text(reader: &mut BufReader<TcpStream>) -> std::io::Result<String> {
    let mut text = String::new();
    loop {
        let mut line = String::new();
        if reader.read_line(&mut line)? == 0 || line == ".\r\n" {
            return Ok(text);
        }
        text.push_str(&line);
    }
}
