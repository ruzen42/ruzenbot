use rand::Rng;

#[derive(Clone, Copy)]
pub enum GameKind {
    AllOrNothing,
    FiftyFifty,
}

pub struct GameResult {
    pub is_win: bool,
    pub new_balance: i64,
}

pub fn play(kind: GameKind, balance: i64) -> GameResult {
    let is_win = rand::rng().random_bool(0.5);
    let new_balance = match (kind, is_win) {
        (GameKind::AllOrNothing, true) => balance.saturating_mul(10),
        (GameKind::AllOrNothing, false) => 0,
        (GameKind::FiftyFifty, true) => balance.saturating_mul(3) / 2,
        (GameKind::FiftyFifty, false) => balance / 2,
    };
    GameResult { is_win, new_balance }
}
