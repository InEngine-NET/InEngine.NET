use std::io::{Read, Write};

use base64::Engine;
use flate2::read::GzDecoder;
use flate2::write::GzEncoder;
use flate2::Compression;

use crate::error::QueueError;

pub fn compress(text: &str) -> Result<String, QueueError> {
    if text.is_empty() {
        return Ok(String::new());
    }
    let mut encoder = GzEncoder::new(Vec::new(), Compression::default());
    encoder
        .write_all(text.as_bytes())
        .map_err(|e| QueueError::Compression {
            message: format!("gzip compression failed: {e}"),
        })?;
    let compressed = encoder.finish().map_err(|e| QueueError::Compression {
        message: format!("gzip finalization failed: {e}"),
    })?;
    Ok(base64::engine::general_purpose::STANDARD.encode(compressed))
}

pub fn decompress(compressed: &str) -> Result<String, QueueError> {
    if compressed.is_empty() {
        return Ok(String::new());
    }
    let bytes = base64::engine::general_purpose::STANDARD
        .decode(compressed)
        .map_err(|e| QueueError::Compression {
            message: format!("base64 decode failed: {e}"),
        })?;
    let mut decoder = GzDecoder::new(&bytes[..]);
    let mut result = String::new();
    decoder
        .read_to_string(&mut result)
        .map_err(|e| QueueError::Compression {
            message: format!("gzip decompression failed: {e}"),
        })?;
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
    fn decompress_invalid_base64_returns_error() {
        // Arrange
        let invalid = "not valid base64!!!@@@";

        // Act
        let result = decompress(invalid);

        // Assert
        assert!(result.is_err());
        let err = result.unwrap_err();
        assert!(err.to_string().contains("base64"));
    }
}
