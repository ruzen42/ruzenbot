pub mod db;
pub mod game;

pub use db::{CasinoDb, CasinoDbError};
pub use game::{play, GameKind, GameResult};

pub const STARTING_BALANCE: i64 = 100;
