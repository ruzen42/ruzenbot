pub mod db;
pub mod game;

<<<<<<< HEAD
pub use db::{BuyBoostOutcome, CasinoDb, CasinoDbError, BOOST_COST};
pub use game::{play, resolve_duel, GameKind};

pub const STARTING_BALANCE: i64 = 1000;
=======
pub use db::{CasinoDb, CasinoDbError};
pub use game::{play, resolve_duel, GameKind, GameResult};

pub const STARTING_BALANCE: i64 = 67;
>>>>>>> 6839d71 (fix)
