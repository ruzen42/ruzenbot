use crate::casino::CasinoDb;
use crate::shell_runner::ShellRunnerClient;
use std::collections::HashMap;
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::{Arc, Mutex};
use teloxide::types::ChatId;

pub struct DuelChallenge {
    pub challenger_id: u64,
    pub challenger_name: String,
    pub opponent_id: u64,
    pub opponent_name: String,
    pub wager: i64,
}

#[derive(Clone)]
pub struct AppState {
    pub casino_db: Arc<CasinoDb>,
    pub shell_runner: Arc<ShellRunnerClient>,
    pub admin_chat_id: ChatId,
    pub duel_challenges: Arc<Mutex<HashMap<u64, DuelChallenge>>>,
    pub duel_counter: Arc<AtomicU64>,
}

impl AppState {
    pub fn next_duel_token(&self) -> u64 {
        self.duel_counter.fetch_add(1, Ordering::SeqCst)
    }
}
