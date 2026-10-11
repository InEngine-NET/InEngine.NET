use std::ffi::{OsStr, OsString};
use std::fs::{self, File};
use std::io::{self, Write};
use std::path::Path;
use std::time::{Duration, SystemTime};

const TEMP_SUFFIX: &str = ".tmp";
const SETTLING_SUFFIX: &str = ".settling";

/// Names created later in the same process sort after earlier ones.
pub(super) fn new_message_name() -> OsString {
    OsString::from(uuid::Uuid::now_v7().simple().to_string())
}

pub(super) fn list_messages(dir: &Path) -> io::Result<Vec<OsString>> {
    let mut names = Vec::new();
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        let name = entry.file_name();
        if is_hidden(&name) {
            continue;
        }
        match entry.file_type() {
            Ok(file_type) if file_type.is_file() => names.push(name),
            Ok(_) => {}
            Err(error) if error.kind() == io::ErrorKind::NotFound => {}
            Err(error) => return Err(error),
        }
    }
    names.sort();
    Ok(names)
}

pub(super) fn unfinished_settles(dir: &Path) -> io::Result<Vec<OsString>> {
    let mut names = Vec::new();
    for entry in fs::read_dir(dir)? {
        let name = entry?.file_name();
        if let Some(original) = strip_hidden_suffix(&name, SETTLING_SUFFIX) {
            names.push(original);
        }
    }
    names.sort();
    Ok(names)
}

pub(super) fn settling_name(name: &OsStr) -> OsString {
    hidden_name(name, SETTLING_SUFFIX)
}

/// Syncs the directory too, so that the new name survives a power loss.
pub(super) fn write_atomic(dir: &Path, name: &OsStr, contents: &[u8]) -> io::Result<()> {
    let temp_path = dir.join(hidden_name(name, TEMP_SUFFIX));
    let mut file = File::create_new(&temp_path)?;
    let written = file
        .write_all(contents)
        .and_then(|()| file.sync_all())
        .and_then(|()| fs::rename(&temp_path, dir.join(name)));
    if written.is_err() {
        let _ = fs::remove_file(&temp_path);
    }
    written?;
    File::open(dir)?.sync_all()
}

pub(super) fn remove_stale_temp_files(dir: &Path, age: Duration) -> io::Result<()> {
    let cutoff = SystemTime::now()
        .checked_sub(age)
        .unwrap_or(SystemTime::UNIX_EPOCH);
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        if strip_hidden_suffix(&entry.file_name(), TEMP_SUFFIX).is_none() {
            continue;
        }
        let modified = match entry.metadata().and_then(|metadata| metadata.modified()) {
            Ok(modified) => modified,
            Err(error) if error.kind() == io::ErrorKind::NotFound => continue,
            Err(error) => return Err(error),
        };
        if modified < cutoff {
            remove_if_present(&entry.path())?;
        }
    }
    Ok(())
}

pub(super) fn remove_settling_files(dir: &Path) -> io::Result<usize> {
    let mut removed = 0;
    for name in unfinished_settles(dir)? {
        if remove_if_present(&dir.join(settling_name(&name)))? {
            removed += 1;
        }
    }
    Ok(removed)
}

pub(super) fn remove_if_present(path: &Path) -> io::Result<bool> {
    match fs::remove_file(path) {
        Ok(()) => Ok(true),
        Err(error) if error.kind() == io::ErrorKind::NotFound => Ok(false),
        Err(error) => Err(error),
    }
}

pub(super) fn rename_if_present(from: &Path, to: &Path) -> io::Result<bool> {
    match fs::rename(from, to) {
        Ok(()) => Ok(true),
        Err(error) if error.kind() == io::ErrorKind::NotFound => Ok(false),
        Err(error) => Err(error),
    }
}

fn is_hidden(name: &OsStr) -> bool {
    name.as_encoded_bytes().first() == Some(&b'.')
}

fn hidden_name(name: &OsStr, suffix: &str) -> OsString {
    let mut hidden = OsString::from(".");
    hidden.push(name);
    hidden.push(suffix);
    hidden
}

fn strip_hidden_suffix(name: &OsStr, suffix: &str) -> Option<OsString> {
    let name = name.to_str()?;
    let original = name.strip_prefix('.')?.strip_suffix(suffix)?;
    (!original.is_empty()).then(|| OsString::from(original))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn names_created_in_sequence_sort_in_sequence() {
        // Arrange
        let count = 1000;

        // Act
        let names: Vec<_> = (0..count).map(|_| new_message_name()).collect();

        // Assert
        let mut sorted = names.clone();
        sorted.sort();
        assert_eq!(names, sorted);
    }

    #[test]
    fn hidden_names_round_trip_to_the_original() {
        // Arrange
        let name = OsString::from("0193abc");

        // Act
        let settling = settling_name(&name);

        // Assert
        assert_eq!(settling, OsString::from(".0193abc.settling"));
        assert_eq!(strip_hidden_suffix(&settling, SETTLING_SUFFIX), Some(name));
        assert_eq!(
            strip_hidden_suffix(OsStr::new(".settling"), SETTLING_SUFFIX),
            None
        );
        assert_eq!(
            strip_hidden_suffix(OsStr::new("plain"), SETTLING_SUFFIX),
            None
        );
    }

    #[test]
    fn write_atomic_leaves_only_the_final_file() {
        // Arrange
        let dir = tempfile::tempdir().expect("temporary directory");
        let name = OsString::from("message");

        // Act
        write_atomic(dir.path(), &name, b"content").expect("write");

        // Assert
        let entries: Vec<_> = fs::read_dir(dir.path())
            .expect("read dir")
            .map(|entry| entry.expect("entry").file_name())
            .collect();
        assert_eq!(entries, vec![name.clone()]);
        assert_eq!(fs::read(dir.path().join(name)).expect("read"), b"content");
    }
}
