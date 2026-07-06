use crate::casino::CasinoDb;
use crate::shell_runner::ShellRunnerClient;
use std::sync::Arc;
use teloxide::types::ChatId;
 
#[derive(Clone)]
pub struct AppState {
    pub casino_db: Arc<CasinoDb>,
    pub shell_runner: Arc<ShellRunnerClient>,
    pub admin_chat_id: ChatId,
}
