use redb::{Database, ReadableTable, TableDefinition};
use serde::{Deserialize, Serialize};
use std::path::Path;
use std::time::{SystemTime, UNIX_EPOCH};
use thiserror::Error;

const USERS: TableDefinition<u64, &str> = TableDefinition::new("users");

const SECONDS_PER_DAY: i64 = 86400;
const DEFAULT_BOOST_PERCENT: i64 = 10;
pub const BOOST_COST: i64 = 10_000;
pub const BOOST_STEP: i64 = 1;

#[derive(Debug, Error)]
pub enum CasinoDbError {
    #[error("database open error: {0}")]
    DatabaseOpen(#[from] redb::DatabaseError),
    #[error("transaction error: {0}")]
    Transaction(#[from] redb::TransactionError),
    #[error("table error: {0}")]
    Table(#[from] redb::TableError),
    #[error("commit error: {0}")]
    Commit(#[from] redb::CommitError),
    #[error("storage error: {0}")]
    Storage(#[from] redb::StorageError),
    #[error("serialization error: {0}")]
    Serde(#[from] serde_json::Error),
    #[error("user {0} is not registered")]
    NotRegistered(u64),
}

fn default_boost_percent() -> i64 {
    DEFAULT_BOOST_PERCENT
}

#[derive(Serialize, Deserialize, Debug, Clone)]
struct UserRecord {
    balance: i64,
    last_accrual_unix: i64,
    #[serde(default = "default_boost_percent")]
    boost_percent: i64,
}

fn now_unix() -> i64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_secs() as i64)
        .unwrap_or(0)
}

fn apply_daily_accrual(record: &mut UserRecord) -> u32 {
    let elapsed = now_unix() - record.last_accrual_unix;
    if elapsed < SECONDS_PER_DAY {
        return 0;
    }
    let days = (elapsed / SECONDS_PER_DAY) as u32;
    let rate = 1.0 + (record.boost_percent as f64 / 100.0);
    for _ in 0..days {
        record.balance = (record.balance as f64 * rate).round() as i64 + 1;
    }
    record.last_accrual_unix += days as i64 * SECONDS_PER_DAY;
    days
}

pub enum BuyBoostOutcome {
    Purchased { new_balance: i64, new_boost_percent: i64 },
    InsufficientFunds { balance: i64 },
}

pub struct CasinoDb {
    db: Database,
}

impl CasinoDb {
    pub fn open(path: impl AsRef<Path>) -> Result<Self, CasinoDbError> {
        log::info!("opening casino db at {:?}", path.as_ref());
        let db = Database::create(path)?;
        let write_txn = db.begin_write()?;
        {
            let _ = write_txn.open_table(USERS)?;
        }
        write_txn.commit()?;
        log::info!("casino db opened successfully");
        Ok(Self { db })
    }

    pub fn is_registered(&self, user_id: u64) -> Result<bool, CasinoDbError> {
        log::debug!("checking registration for user {user_id}");
        let read_txn = self.db.begin_read()?;
        let table = read_txn.open_table(USERS)?;
        Ok(table.get(user_id)?.is_some())
    }

    pub fn register(&self, user_id: u64, starting_balance: i64) -> Result<bool, CasinoDbError> {
        log::info!("register attempt: user={user_id} starting_balance={starting_balance}");
        let write_txn = self.db.begin_write()?;
        let registered = {
            let mut table = write_txn.open_table(USERS)?;
            if table.get(user_id)?.is_some() {
                log::info!("register: user={user_id} already registered");
                false
            } else {
                let record = UserRecord {
                    balance: starting_balance,
                    last_accrual_unix: now_unix(),
                    boost_percent: DEFAULT_BOOST_PERCENT,
                };
                let json = serde_json::to_string(&record)?;
                table.insert(user_id, json.as_str())?;
                log::info!("register: user={user_id} registered with balance={starting_balance}");
                true
            }
        };
        write_txn.commit()?;
        Ok(registered)
    }

    pub fn get_balance(&self, user_id: u64) -> Result<Option<i64>, CasinoDbError> {
        log::debug!("get_balance (with daily accrual) for user={user_id}");
        let read_txn = self.db.begin_read()?;
        let table = read_txn.open_table(USERS)?;
        let Some(raw) = table.get(user_id)? else {
            log::debug!("get_balance: user={user_id} not registered");
            return Ok(None);
        };
        let mut record: UserRecord = serde_json::from_str(raw.value())?;
        drop(table);
        drop(read_txn);

        let days = apply_daily_accrual(&mut record);
        if days > 0 {
            log::info!(
                "daily accrual applied: user={user_id} days={days} new_balance={}",
                record.balance
            );
            let write_txn = self.db.begin_write()?;
            {
                let mut table = write_txn.open_table(USERS)?;
                let json = serde_json::to_string(&record)?;
                table.insert(user_id, json.as_str())?;
            }
            write_txn.commit()?;
        }

        Ok(Some(record.balance))
    }

    pub fn set_balance(&self, user_id: u64, balance: i64) -> Result<(), CasinoDbError> {
        log::info!("set_balance: user={user_id} balance={balance}");
        let write_txn = self.db.begin_write()?;
        {
            let mut table = write_txn.open_table(USERS)?;
            let mut record = match table.get(user_id)? {
                Some(raw) => serde_json::from_str::<UserRecord>(raw.value())?,
                None => UserRecord {
                    balance: 0,
                    last_accrual_unix: now_unix(),
                    boost_percent: DEFAULT_BOOST_PERCENT,
                },
            };
            record.balance = balance;
            let json = serde_json::to_string(&record)?;
            table.insert(user_id, json.as_str())?;
        }
        write_txn.commit()?;
        log::info!("set_balance committed: user={user_id} balance={balance}");
        Ok(())
    }

    pub fn get_boost_percent(&self, user_id: u64) -> Result<Option<i64>, CasinoDbError> {
        log::debug!("get_boost_percent: user={user_id}");
        let read_txn = self.db.begin_read()?;
        let table = read_txn.open_table(USERS)?;
        let Some(raw) = table.get(user_id)? else {
            return Ok(None);
        };
        let record: UserRecord = serde_json::from_str(raw.value())?;
        Ok(Some(record.boost_percent))
    }

    pub fn buy_boost(&self, user_id: u64) -> Result<BuyBoostOutcome, CasinoDbError> {
        log::info!("buy_boost: user={user_id}");
        let write_txn = self.db.begin_write()?;
        let outcome = {
            let mut table = write_txn.open_table(USERS)?;
            let Some(raw) = table.get(user_id)? else {
                return Err(CasinoDbError::NotRegistered(user_id));
            };
            let mut record: UserRecord = serde_json::from_str(raw.value())?;

            if record.balance < BOOST_COST {
                log::info!(
                    "buy_boost: user={user_id} insufficient balance={} cost={BOOST_COST}",
                    record.balance
                );
                BuyBoostOutcome::InsufficientFunds { balance: record.balance }
            } else {
                record.balance -= BOOST_COST;
                record.boost_percent += BOOST_STEP;
                let new_balance = record.balance;
                let new_boost = record.boost_percent;
                table.insert(user_id, serde_json::to_string(&record)?.as_str())?;
                log::info!(
                    "buy_boost: user={user_id} new_balance={new_balance} new_boost_percent={new_boost}"
                );
                BuyBoostOutcome::Purchased { new_balance, new_boost_percent: new_boost }
            }
        };
        write_txn.commit()?;
        Ok(outcome)
    }

    pub fn reset_boost(&self, user_id: u64) -> Result<bool, CasinoDbError> {
        log::info!("reset_boost: user={user_id}");
        let write_txn = self.db.begin_write()?;
        let found = {
            let mut table = write_txn.open_table(USERS)?;
            match table.get(user_id)? {
                Some(raw) => {
                    let mut record: UserRecord = serde_json::from_str(raw.value())?;
                    record.boost_percent = DEFAULT_BOOST_PERCENT;
                    table.insert(user_id, serde_json::to_string(&record)?.as_str())?;
                    true
                }
                None => false,
            }
        };
        write_txn.commit()?;
        log::info!("reset_boost: user={user_id} found={found}");
        Ok(found)
    }

    pub fn top_players(&self, limit: usize) -> Result<Vec<(u64, i64)>, CasinoDbError> {
        log::debug!("top_players: limit={limit}");
        let read_txn = self.db.begin_read()?;
        let table = read_txn.open_table(USERS)?;

        let mut entries: Vec<(u64, i64)> = Vec::new();
        for row in table.iter()? {
            let (key, value) = row?;
            let user_id = key.value();
            let mut record: UserRecord = serde_json::from_str(value.value())?;
            apply_daily_accrual(&mut record);
            entries.push((user_id, record.balance));
        }

        entries.sort_by(|a, b| b.1.cmp(&a.1));
        entries.truncate(limit);
        log::debug!("top_players: found {} entries after truncate", entries.len());
        Ok(entries)
    }

    pub fn transfer(&self, from: u64, to: u64, amount: i64) -> Result<(), CasinoDbError> {
        log::info!("transfer: from={from} to={to} amount={amount}");
        let write_txn = self.db.begin_write()?;
        {
            let mut table = write_txn.open_table(USERS)?;

            let mut from_record: UserRecord = match table.get(from)? {
                Some(raw) => serde_json::from_str(raw.value())?,
                None => {
                    log::warn!("transfer failed: sender {from} not registered");
                    return Err(CasinoDbError::NotRegistered(from));
                }
            };
            let mut to_record: UserRecord = match table.get(to)? {
                Some(raw) => serde_json::from_str(raw.value())?,
                None => {
                    log::warn!("transfer failed: recipient {to} not registered");
                    return Err(CasinoDbError::NotRegistered(to));
                }
            };

            from_record.balance -= amount;
            to_record.balance += amount;

            table.insert(from, serde_json::to_string(&from_record)?.as_str())?;
            table.insert(to, serde_json::to_string(&to_record)?.as_str())?;
        }
        write_txn.commit()?;
        log::info!("transfer committed: from={from} to={to} amount={amount}");
        Ok(())
    }
}
