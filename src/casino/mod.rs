pub mod db;
pub mod game;

pub use db::CasinoDb;
pub use game::{play, GameKind};

pub const STARTING_BALANCE: i64 = 100;
