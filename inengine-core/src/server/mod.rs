pub(crate) mod consumers;
/// The server that runs the scheduler and the queue consumers together.
pub mod host;
/// Runs a scheduled command with its pings, output files, mail, and time limit.
pub mod lifecycle;
/// Sends the HTTP pings and the mail that a scheduled command asks for.
pub mod notify;
