use serde::{Deserialize, Serialize};
use std::time::Duration;

#[derive(Serialize)]
struct ShellRequest<'a> {
    command: &'a str,
}

#[derive(Deserialize, Debug)]
pub struct ShellResponse {
    pub output: Option<String>,
    pub err: Option<String>,
    #[serde(rename = "exitCode")]
    pub exit_code: i32,
}

pub struct ShellRunnerClient {
    client: reqwest::Client,
    base_url: String,
}

impl ShellRunnerClient {
    pub fn new(base_url: impl Into<String>) -> Self {
        let client = reqwest::Client::builder()
            .timeout(Duration::from_secs(10))
            .build()
            .expect("failed to build reqwest client");
        Self { client, base_url: base_url.into() }
    }

    pub async fn execute(&self, command: &str) -> anyhow::Result<ShellResponse> {
        let resp = self
            .client
            .post(&self.base_url)
            .json(&ShellRequest { command })
            .send()
            .await?
            .error_for_status()?;
        Ok(resp.json::<ShellResponse>().await?)
    }
}
