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
use std::collections::HashMap;
use std::sync::atomic::AtomicU64;
use std::sync::{Arc, Mutex};
use teloxide::dispatching::UpdateFilterExt;
use teloxide::prelude::*;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    pretty_env_logger::init();
    log::info!("starting ruzenbot-rs");

    let config = Config::from_env()?;
    log::info!("config loaded: admin_chat_id={} db_path={} shell_runner_url={}",
        config.admin_chat_id.0, config.db_path, config.shell_runner_url);

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
        duel_challenges: Arc::new(Mutex::new(HashMap::new())),
        duel_counter: Arc::new(AtomicU64::new(0)),
    };

    let handler = dptree::entry()
        .branch(
            Update::filter_message()
                .filter_command::<Command>()
                .endpoint(commands::dispatch),
        )
        .branch(
            Update::filter_callback_query().endpoint(commands::handle_duel_callback),
        );

    log::info!("dispatcher starting");

    Dispatcher::builder(bot, handler)
        .dependencies(dptree::deps![state])
        .enable_ctrlc_handler()
        .build()
        .dispatch()
        .await;

    log::info!("dispatcher stopped");

    Ok(())
}
