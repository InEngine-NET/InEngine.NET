use std::io::{BufRead, BufReader, Write};
use std::net::TcpListener;

/// An HTTP server that answers every request with `status` and an empty body.
pub(crate) struct StatusServer {
    pub url: String,
}

impl StatusServer {
    pub(crate) fn start(status: u16) -> Self {
        let listener = TcpListener::bind("127.0.0.1:0").expect("bind the HTTP server");
        let url = format!("http://{}/", listener.local_addr().expect("address"));
        std::thread::spawn(move || {
            for stream in listener.incoming() {
                let Ok(stream) = stream else { return };
                let mut reader = BufReader::new(stream);
                let mut line = String::new();
                while reader.read_line(&mut line).is_ok_and(|read| read > 0) {
                    if line == "\r\n" {
                        break;
                    }
                    line.clear();
                }
                let response = format!(
                    "HTTP/1.1 {status} Status\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                );
                let _ = reader.get_mut().write_all(response.as_bytes());
            }
        });
        Self { url }
    }
}
