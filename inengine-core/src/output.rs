use crossterm::style::{Color, ResetColor, SetForegroundColor};
use indicatif::ProgressBar;

/// A buffer that a command writes its output to.
///
/// Each write method adds to the buffer and returns `&mut Self`, so you can chain calls. Call
/// [`Output::print`] to send the buffer to standard output, or [`Output::take_buffer`] to get it as
/// a string.
pub struct Output {
    buffer: Vec<u8>,
    use_color: bool,
    progress_bar: Option<ProgressBar>,
}

impl Output {
    /// Creates an empty buffer. When `use_color` is true, colored writes add ANSI color codes and
    /// the progress bar is drawn.
    pub fn new(use_color: bool) -> Self {
        Self {
            buffer: Vec::new(),
            use_color,
            progress_bar: None,
        }
    }

    /// The flag given to [`Output::new`].
    pub fn uses_color(&self) -> bool {
        self.use_color
    }

    /// Adds `text` and a newline without color.
    pub fn line(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, None, true)
    }

    /// Adds `text` in green and a newline.
    pub fn info_line(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Green), true)
    }

    /// Adds `text` in yellow and a newline.
    pub fn warning_line(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Yellow), true)
    }

    /// Adds `text` in red and a newline.
    pub fn error_line(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Red), true)
    }

    /// Adds `text` without color or a newline.
    pub fn text(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, None, false)
    }

    /// Adds `text` in green without a newline.
    pub fn info(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Green), false)
    }

    /// Adds `text` in yellow without a newline.
    pub fn warning(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Yellow), false)
    }

    /// Adds `text` in red without a newline.
    pub fn error(&mut self, text: &str) -> &mut Self {
        self.write_colored(text, Some(Color::Red), false)
    }

    /// Adds `count` newlines.
    pub fn newlines(&mut self, count: usize) -> &mut Self {
        self.buffer.extend(std::iter::repeat_n(b'\n', count));
        self
    }

    /// Adds `text` in `color` and a newline. The color is left out when color is off.
    pub fn colored_line(&mut self, text: &str, color: Color) -> &mut Self {
        self.write_colored(text, Some(color), true)
    }

    /// Adds `text` in `color` without a newline. The color is left out when color is off.
    pub fn colored_text(&mut self, text: &str, color: Color) -> &mut Self {
        self.write_colored(text, Some(color), false)
    }

    /// Starts a progress bar that counts up to `max`. When color is off, the bar is hidden.
    pub fn set_progress_max(&mut self, max: u64) {
        if self.use_color {
            self.progress_bar = Some(ProgressBar::new(max));
        } else {
            self.progress_bar = Some(ProgressBar::hidden());
        }
    }

    /// Moves the progress bar to `current`. Does nothing before [`Output::set_progress_max`].
    pub fn update_progress(&mut self, current: u64) {
        if let Some(ref pb) = self.progress_bar {
            pb.set_position(current);
        }
    }

    /// Returns the buffered text and empties the buffer. Invalid UTF-8 is replaced with U+FFFD.
    pub fn take_buffer(&mut self) -> String {
        let content = String::from_utf8_lossy(&self.buffer).to_string();
        self.buffer.clear();
        content
    }

    /// Writes the buffer to standard output, flushes it, and empties the buffer. Write errors are
    /// ignored.
    pub fn print(&mut self) {
        use std::io::Write;
        if !self.buffer.is_empty() {
            let _ = std::io::stdout().write_all(&self.buffer);
            let _ = std::io::stdout().flush();
            self.buffer.clear();
        }
    }

    fn write_colored(&mut self, text: &str, color: Option<Color>, with_newline: bool) -> &mut Self {
        use crossterm::Command;

        if self.use_color {
            if let Some(c) = color {
                let mut ansi = String::new();
                let _ = SetForegroundColor(c).write_ansi(&mut ansi);
                self.buffer.extend_from_slice(ansi.as_bytes());
                self.buffer.extend_from_slice(text.as_bytes());
                ansi.clear();
                let _ = ResetColor.write_ansi(&mut ansi);
                self.buffer.extend_from_slice(ansi.as_bytes());
            } else {
                self.buffer.extend_from_slice(text.as_bytes());
            }
        } else {
            self.buffer.extend_from_slice(text.as_bytes());
        }

        if with_newline {
            self.buffer.push(b'\n');
        }

        self
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn info_line_writes_green_with_newline() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.info_line("hello");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("hello"));
        assert!(buf.contains("\x1b["), "expected ANSI escape sequence");
        assert!(buf.ends_with('\n'));
    }

    #[test]
    fn text_writes_without_newline() {
        // Arrange
        let mut output = Output::new(false);

        // Act
        output.text("hello");

        // Assert
        let buf = output.take_buffer();
        assert_eq!(buf, "hello");
        assert!(!buf.ends_with('\n'));
    }

    #[test]
    fn take_buffer_returns_contents_and_clears() {
        // Arrange
        let mut output = Output::new(false);
        output.line("first");

        // Act
        let first = output.take_buffer();
        let second = output.take_buffer();

        // Assert
        assert_eq!(first, "first\n");
        assert_eq!(second, "");
    }

    #[test]
    fn method_chaining_works() {
        // Arrange
        let mut output = Output::new(false);

        // Act
        output.info_line("a").warning_line("b");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("a\n"));
        assert!(buf.contains("b\n"));
    }

    #[test]
    fn color_suppressed_when_use_color_false() {
        // Arrange
        let mut output = Output::new(false);

        // Act
        output.info_line("hello");

        // Assert
        let buf = output.take_buffer();
        assert_eq!(buf, "hello\n");
        assert!(!buf.contains("\x1b["));
    }

    #[test]
    fn newlines_writes_correct_count() {
        // Arrange
        let mut output = Output::new(false);

        // Act
        output.newlines(3);

        // Assert
        let buf = output.take_buffer();
        assert_eq!(buf, "\n\n\n");
    }

    #[test]
    fn colored_line_uses_specified_color() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.colored_line("test", Color::Cyan);

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("test"));
        assert!(buf.ends_with('\n'));
    }

    #[test]
    fn colored_text_no_newline() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.colored_text("test", Color::Red);

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("test"));
        assert!(!buf.ends_with('\n'));
    }

    #[test]
    fn error_line_writes_red() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.error_line("bad");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("bad"));
    }

    #[test]
    fn warning_line_writes_yellow() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.warning_line("caution");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("caution"));
    }

    #[test]
    fn info_green_no_newline() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.info("note");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("note"));
        assert!(!buf.ends_with('\n'));
    }

    #[test]
    fn warning_yellow_no_newline() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.warning("warn");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("warn"));
        assert!(!buf.ends_with('\n'));
    }

    #[test]
    fn progress_bar_does_not_panic() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.set_progress_max(100);
        output.update_progress(50);
        output.update_progress(100);

        // Assert (no panic)
    }

    #[test]
    fn progress_suppressed_when_no_color() {
        // Arrange
        let mut output = Output::new(false);

        // Act
        output.set_progress_max(100);
        output.update_progress(50);

        // Assert (no panic, hidden bar)
    }

    #[test]
    fn error_red_no_newline() {
        // Arrange
        let mut output = Output::new(true);

        // Act
        output.error("err");

        // Assert
        let buf = output.take_buffer();
        assert!(buf.contains("\x1b["));
        assert!(buf.contains("err"));
        assert!(!buf.ends_with('\n'));
    }
}
