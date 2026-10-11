use std::sync::OnceLock;
use std::time::Duration;

use lettre::message::header::ContentType;
use lettre::transport::smtp::authentication::Credentials;
use lettre::{AsyncSmtpTransport, AsyncTransport, Message, Tokio1Executor};

use crate::config::{MailEncryption, MailSettings};
use crate::error::CommandError;

const PING_TIMEOUT: Duration = Duration::from_secs(30);

/// Shared by every ping. It keeps no idle connection, because a connection that outlives the
/// runtime that opened it cannot be used again.
static HTTP_CLIENT: OnceLock<reqwest::Client> = OnceLock::new();

fn http_client() -> Result<&'static reqwest::Client, CommandError> {
    if let Some(client) = HTTP_CLIENT.get() {
        return Ok(client);
    }
    let client = reqwest::Client::builder()
        .timeout(PING_TIMEOUT)
        .pool_max_idle_per_host(0)
        .build()
        .map_err(|e| CommandError::failed_with("could not create the HTTP client", e))?;
    Ok(HTTP_CLIENT.get_or_init(|| client))
}

/// Sends a GET request to `url`. A response with an error status fails.
pub async fn ping(url: &str) -> Result<(), CommandError> {
    http_client()?
        .get(url)
        .send()
        .await
        .and_then(reqwest::Response::error_for_status)
        .map_err(|e| CommandError::failed_with(format!("ping to {url} failed"), e))?;
    Ok(())
}

fn mail_transport(
    settings: &MailSettings,
) -> Result<AsyncSmtpTransport<Tokio1Executor>, CommandError> {
    let builder = match settings.encryption {
        MailEncryption::Plain => Ok(AsyncSmtpTransport::<Tokio1Executor>::builder_dangerous(
            &settings.host,
        )),
        MailEncryption::StartTls => {
            AsyncSmtpTransport::<Tokio1Executor>::starttls_relay(&settings.host)
        }
        MailEncryption::Tls => AsyncSmtpTransport::<Tokio1Executor>::relay(&settings.host),
    }
    .map_err(|e| CommandError::failed_with("could not set up the mail transport", e))?;
    let mut builder = builder.port(settings.port);
    if !settings.username.is_empty() {
        builder = builder.credentials(Credentials::new(
            settings.username.clone(),
            settings.password.clone(),
        ));
    }
    Ok(builder.build())
}

/// Sends `body` to `to` through the transport that `mail_settings.encryption` selects.
pub async fn send_email(
    mail_settings: &MailSettings,
    to: &str,
    subject: &str,
    body: &str,
) -> Result<(), CommandError> {
    let email = Message::builder()
        .from(
            mail_settings
                .from
                .parse()
                .map_err(|e: lettre::address::AddressError| {
                    CommandError::failed_with(
                        format!("invalid from address: {}", mail_settings.from),
                        e,
                    )
                })?,
        )
        .to(to.parse().map_err(|e: lettre::address::AddressError| {
            CommandError::failed_with(format!("invalid to address: {to}"), e)
        })?)
        .subject(subject)
        .header(ContentType::TEXT_PLAIN)
        .body(body.to_string())
        .map_err(|e| CommandError::failed_with("failed to build email message", e))?;

    mail_transport(mail_settings)?
        .send(email)
        .await
        .map_err(|e| CommandError::failed_with("failed to send email", e))?;

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::commands::test_helpers::make_ctx;

    #[test]
    fn the_http_client_is_built_once() {
        // Arrange
        let first = http_client().expect("client") as *const reqwest::Client;

        // Act
        let second = http_client().expect("client") as *const reqwest::Client;

        // Assert
        assert_eq!(first, second);
    }

    #[test]
    fn each_encryption_builds_a_transport() {
        // Arrange
        let mut settings = make_ctx().settings.mail.clone();
        let encryptions = [
            MailEncryption::Plain,
            MailEncryption::StartTls,
            MailEncryption::Tls,
        ];

        // Act
        let built: Vec<bool> = encryptions
            .into_iter()
            .map(|encryption| {
                settings.encryption = encryption;
                mail_transport(&settings).is_ok()
            })
            .collect();

        // Assert
        assert_eq!(built, [true, true, true]);
    }

    #[tokio::test]
    async fn ping_to_a_closed_port_returns_error() {
        // Arrange
        let listener = std::net::TcpListener::bind("127.0.0.1:0").expect("bind");
        let url = format!("http://{}/", listener.local_addr().expect("address"));
        drop(listener);

        // Act
        let result = ping(&url).await;

        // Assert
        let error = result.expect_err("nothing listens there");
        assert_eq!(
            error.to_string(),
            format!("command failed: ping to {url} failed")
        );
    }
}
