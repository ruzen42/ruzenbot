use teloxide::types::ChatId;
 
pub struct Config {
    pub token: String,
    pub admin_chat_id: ChatId,
    pub db_path: String,
    pub shell_runner_url: String,
}
 
impl Config {
    pub fn from_env() -> anyhow::Result<Self> {
        let token = std::env::var("TOKEN")
            .map_err(|_| anyhow::anyhow!("TOKEN env var is not set"))?;
 
        let admin_chat_id = std::env::var("ADMIN_CHAT_ID")
            .map_err(|_| anyhow::anyhow!("ADMIN_CHAT_ID env var is not set"))?
            .parse::<i64>()
            .map(ChatId)
            .map_err(|_| anyhow::anyhow!("ADMIN_CHAT_ID must be a valid i64"))?;
 
        let db_path = std::env::var("DB_PATH").unwrap_or_else(|_| "casino.redb".to_string());

        let shell_runner_url = std::env::var("SHELL_RUNNER_URL")
            .unwrap_or_else(|_| "http://shellrunner:8080/api".to_string());
 
        Ok(Self { token, admin_chat_id, db_path, shell_runner_url })
    }
}
