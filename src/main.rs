mod casino;
mod commands;
mod config;
mod shell_runner;
mod state;
 
use casino::CasinoDb;
use commands::Command;
use config::Config;
use shell_runner::ShellRunnerClient;
use state::AppState;
use std::sync::Arc;
use teloxide::prelude::*;
 
#[tokio::main]
async fn main() -> anyhow::Result<()> {
    pretty_env_logger::init();
    log::info!("Starting ruzenbot-rs...");
 
    let config = Config::from_env()?;
    let bot = Bot::new(&config.token);
 
    let casino_db = Arc::new(
        CasinoDb::open(&config.db_path)
            .map_err(|e| anyhow::anyhow!("failed to open casino db: {e}"))?,
    );
    let shell_runner = Arc::new(ShellRunnerClient::new(&config.shell_runner_url));
 
    let state = AppState {
        casino_db,
        shell_runner,
        admin_chat_id: config.admin_chat_id,
    };
 
    Command::repl(bot, move |bot: Bot, msg: Message, cmd: Command| {
        let state = state.clone();
        async move { commands::dispatch(bot, msg, cmd, state).await }
    })
    .await;
 
    Ok(())
}
 
