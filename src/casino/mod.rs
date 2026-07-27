pub mod db;
pub mod game;

pub use db::{BuyBoostOutcome, CasinoDb, CasinoDbError, BOOST_COST, BOOST_STEP};
pub use game::{play, resolve_duel, GameKind, GameResult};

pub const STARTING_BALANCE: i64 = 100;
