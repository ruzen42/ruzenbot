use rand::Rng;

#[derive(Clone, Copy)]
pub enum GameKind {
    AllOrNothing,
    FiftyFifty,
    TwentyEighteen,
    Coinflip(i64),
}

pub struct GameResult {
    pub is_win: bool,
    pub new_balance: i64,
}

pub fn play(kind: GameKind, balance: i64) -> GameResult {
    let is_win = match kind {
        GameKind::AllOrNothing | GameKind::FiftyFifty | GameKind::Coinflip(_) => {
            rand::rng().random_bool(0.5)
        }
        GameKind::TwentyEighteen => rand::rng().random_bool(0.2),
    };
    let new_balance = match (kind, is_win) {
        (GameKind::AllOrNothing, true) => balance.saturating_mul(10),
        (GameKind::AllOrNothing, false) => 0,
        (GameKind::FiftyFifty, true) => balance.saturating_mul(3) / 2,
        (GameKind::FiftyFifty, false) => balance / 2,
        (GameKind::TwentyEighteen, true) => balance.saturating_mul(10) / 9,
        (GameKind::TwentyEighteen, false) => balance / 5,
        (GameKind::Coinflip(bet), true) => balance.saturating_add(bet),
        (GameKind::Coinflip(bet), false) => balance.saturating_sub(bet),
    };
    log::info!(
        "game played: kind_won={is_win} old_balance={balance} new_balance={new_balance}"
    );
    GameResult { is_win, new_balance }
}

pub fn resolve_duel(balance_a: i64, balance_b: i64, wager: i64) -> bool {
    let a_wins = rand::rng().random_bool(0.5);
    log::info!(
        "duel resolved: wager={wager} a_balance={balance_a} b_balance={balance_b} a_wins={a_wins}"
    );
    a_wins
}
