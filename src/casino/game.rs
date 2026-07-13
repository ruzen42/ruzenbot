use rand::Rng;

#[derive(Clone, Copy)]
pub enum GameKind {
    AllOrNothing,
    FiftyFifty,
    TwentyEighteen,
}

pub struct GameResult {
    pub is_win: bool,
    pub new_balance: i64,
}

pub fn play(kind: GameKind, balance: i64) -> GameResult {
    let is_win = match kind {
        GameKind::AllOrNothing | GameKind::FiftyFifty => rand::rng().random_bool(0.5),
        GameKind::TwentyEighteen => rand::rng().random_bool(0.2),
    };
    let new_balance = match (kind, is_win) {
        (GameKind::AllOrNothing, true) => balance.saturating_mul(10),
        (GameKind::AllOrNothing, false) => 0,
        (GameKind::FiftyFifty, true) => balance.saturating_mul(3) / 2,
        (GameKind::FiftyFifty, false) => balance / 2,
        (GameKind::TwentyEighteen, true) => balance.saturating_mul(10) / 9,
        (GameKind::TwentyEighteen, false) => balance / 5
    };
    GameResult { is_win, new_balance }
}
