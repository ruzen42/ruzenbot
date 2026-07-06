use redb::{Database, ReadableTable, TableDefinition};
use std::path::Path;
use thiserror::Error;
 
const BALANCES: TableDefinition<u64, i64> = TableDefinition::new("balances");
 
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
}
 
pub struct CasinoDb {
    db: Database,
}
 
impl CasinoDb {
    pub fn open(path: impl AsRef<Path>) -> Result<Self, CasinoDbError> {
        let db = Database::create(path)?;
        let write_txn = db.begin_write()?;
        {
            let _ = write_txn.open_table(BALANCES)?;
        }
        write_txn.commit()?;
        Ok(Self { db })
    }
 
    pub fn get_balance(&self, user_id: u64) -> Result<Option<i64>, CasinoDbError> {
        let read_txn = self.db.begin_read()?;
        let table = read_txn.open_table(BALANCES)?;
        Ok(table.get(user_id)?.map(|v| v.value()))
    }
 
    pub fn is_registered(&self, user_id: u64) -> Result<bool, CasinoDbError> {
        Ok(self.get_balance(user_id)?.is_some())
    }
 
    pub fn register(&self, user_id: u64, starting_balance: i64) -> Result<bool, CasinoDbError> {
        let write_txn = self.db.begin_write()?;
        let registered = {
            let mut table = write_txn.open_table(BALANCES)?;
            if table.get(user_id)?.is_some() {
                false
            } else {
                table.insert(user_id, starting_balance)?;
                true
            }
        };
        write_txn.commit()?;
        Ok(registered)
    }
 
    pub fn set_balance(&self, user_id: u64, balance: i64) -> Result<(), CasinoDbError> {
        let write_txn = self.db.begin_write()?;
        {
            let mut table = write_txn.open_table(BALANCES)?;
            table.insert(user_id, balance)?;
        }
        write_txn.commit()?;
        Ok(())
    }
}
