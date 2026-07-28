pub mod db;
pub mod game;

pub use db::{BuyBoostOutcome, CasinoDb, CasinoDbError, BOOST_COST};
pub use game::{play, resolve_duel, GameKind};

pub const STARTING_BALANCE: i64 = 1000;
