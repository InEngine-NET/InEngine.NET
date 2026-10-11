use std::io::{Read, Write};

use base64::Engine;
use flate2::Compression;
use flate2::read::GzDecoder;
use flate2::write::GzEncoder;

use crate::error::QueueError;

pub(super) fn compress(text: &str) -> Result<String, QueueError> {
    if text.is_empty() {
        return Ok(String::new());
    }
    let mut encoder = GzEncoder::new(Vec::new(), Compression::default());
    encoder
        .write_all(text.as_bytes())
        .map_err(QueueError::Compress)?;
    let compressed = encoder.finish().map_err(QueueError::Compress)?;
    Ok(base64::engine::general_purpose::STANDARD.encode(compressed))
}

pub(super) fn decompress(compressed: &str) -> Result<String, QueueError> {
    if compressed.is_empty() {
        return Ok(String::new());
    }
    let bytes = base64::engine::general_purpose::STANDARD.decode(compressed)?;
    let mut decoder = GzDecoder::new(&bytes[..]);
    let mut result = String::new();
    decoder
        .read_to_string(&mut result)
        .map_err(QueueError::Decompress)?;
    Ok(result)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn compress_and_decompress_roundtrip() {
        // Arrange
        let original = "hello world, this is a test of compression";

        // Act
        let compressed = compress(original).unwrap();
        let decompressed = decompress(&compressed).unwrap();

        // Assert
        assert_eq!(decompressed, original);
        assert_ne!(compressed, original);
    }

    #[test]
    fn compress_empty_string_returns_empty() {
        // Arrange
        let original = "";

        // Act
        let compressed = compress(original).unwrap();

        // Assert
        assert_eq!(compressed, "");
    }

    #[test]
    fn a_payload_that_is_not_base64_keeps_its_source() {
        // Arrange
        let invalid = "not valid base64!!!@@@";

        // Act
        let result = decompress(invalid);

        // Assert
        let error = result.expect_err("the payload is not base64");
        assert!(
            matches!(error, QueueError::PayloadNotBase64(_)),
            "{error:?}"
        );
        let source = std::error::Error::source(&error).expect("the error keeps its source");
        assert!(source.is::<base64::DecodeError>());
    }

    #[test]
    fn a_payload_that_is_not_gzip_keeps_its_source() {
        // Arrange
        let not_gzip = base64::engine::general_purpose::STANDARD.encode(b"plain text");

        // Act
        let result = decompress(&not_gzip);

        // Assert
        let error = result.expect_err("the payload is not gzip");
        assert!(matches!(error, QueueError::Decompress(_)), "{error:?}");
        let source = std::error::Error::source(&error).expect("the error keeps its source");
        assert!(source.is::<std::io::Error>());
    }
}
