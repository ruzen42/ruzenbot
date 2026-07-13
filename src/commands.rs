use crate::casino::{self, GameKind};
use crate::state::AppState;
use teloxide::prelude::*;
use teloxide::types::ReplyParameters;
use teloxide::utils::command::BotCommands;

pub type HandlerResult = Result<(), teloxide::RequestError>;

#[derive(BotCommands, Clone)]
#[command(
    rename_rule = "lowercase",
    description = "Доступные команды:"
)]
pub enum Command {
    #[command(description = "показать список команд")]
    Man,
    #[command(description = "проверить, жив ли бот")]
    Ping,
    #[command(description = "узнать свой telegram id")]
    Id,
    #[command(description = "оценить вещь по случайной шкале (текст после команды или в reply)")]
    Rate,
    #[command(description = "зарегистрироваться в казино")]
    Register,
    #[command(description = "узнать баланс казино")]
    Balance,
    #[command(description = "сыграть в 'всё или ничего' (x10 / 0)")]
    AllOrNothing,
    #[command(description = "сыграть в 'пятьдесят на пятьдесят' (x1.5 / x0.5)")]
    FiftyFifty,
    #[command(description = "сыграть в 'двадцать на восьмьдесят' (x1.1 / x0.2)")]
    TwentyEighteen,
    #[command(description = "выполнить shell-команду через shell-runner (только админ)")]
    Sent,
    Give
}

fn extract_argument(msg: &Message) -> Option<String> {
    let inline = msg
        .text()
        .and_then(|t| t.split_once(char::is_whitespace))
        .map(|(_, rest)| rest.trim().to_string())
        .filter(|s| !s.is_empty());

    inline.or_else(|| {
        msg.reply_to_message()
            .and_then(|r| r.text())
            .map(|s| s.trim().to_string())
            .filter(|s| !s.is_empty())
    })
}

async fn reply(bot: &Bot, msg: &Message, text: impl Into<String>) -> HandlerResult {
    bot.send_message(msg.chat.id, text)
        .reply_parameters(ReplyParameters::new(msg.id))
        .await?;
    Ok(())
}

pub async fn dispatch(
    bot: Bot,
    msg: Message,
    cmd: Command,
    state: AppState,
) -> HandlerResult {
    match cmd {
        Command::Man => handle_man(bot, msg).await,
        Command::Ping => handle_ping(bot, msg).await,
        Command::Id => handle_id(bot, msg).await,
        Command::Rate => handle_rate(bot, msg).await,
        Command::Register => handle_register(bot, msg, state).await,
        Command::Balance => handle_balance(bot, msg, state).await,
        Command::AllOrNothing => handle_game(bot, msg, state, GameKind::AllOrNothing).await,
        Command::FiftyFifty => handle_game(bot, msg, state, GameKind::FiftyFifty).await,
        Command::TwentyEighteen => handle_game(bot, msg, state, GameKind::TwentyEighteen).await,
        Command::Sent => handle_sent(bot, msg, state).await,
        Command::Give => handle_give(bot, msg, state).await,
    }
}



async fn handle_give(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(sender) = msg.from.as_ref() else {
        return reply(&bot, &msg, "Не удалось определить отправителя").await;
    };
    let sender_id = sender.id.0;
    let Some(target_msg) = msg.reply_to_message() else {
        return reply(&bot, &msg, "Ответь на сообщение того, кому хочешь передать деньги: /give <сумма>").await;
    };
    let Some(recipient) = target_msg.from.as_ref() else {
        return reply(&bot, &msg, "Не удалось определить получателя").await;
    };
    let recipient_id = recipient.id.0;
    let is_admin = sender_id == state.admin_chat_id.0 as u64;

    if recipient_id == sender_id && !is_admin {
        return reply(&bot, &msg, "Себе передавать деньги нельзя").await;
    };

    let Some(amount_str) = msg.text().and_then(|t| t.split_once(char::is_whitespace)).map(|(_, rest)| rest.trim()) else {
        return reply(&bot, &msg, "Использование: /give <сумма> (ответом на сообщение получателя)").await;
    };

    let Ok(amount) = amount_str.parse::<i64>() else {
        return reply(&bot, &msg, "Сумма должна быть целым положительным числом").await;
    };

    if amount <= 0 {
        return reply(&bot, &msg, "Сумма должна быть положительной").await;
    };

    let recipient_balance = match state.casino_db.get_balance(recipient_id) {
        Ok(Some(b)) => b, Ok(None) => return reply(&bot, &msg, "Получатель ещё не зарегистрирован (/register)").await, Err(e) => {
            log::error!("casino get_balance failed for recipient {recipient_id}: {e}"); return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await; }
    };

    if is_admin {
        if let Err(e) = state.casino_db.set_balance(recipient_id, recipient_balance + amount) {
            log::error!("casino set_balance failed for recipient {recipient_id}: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
        return reply(&bot, &msg, format!("Админ выдал {amount} пользователю. Новый баланс получателя: {}", recipient_balance + amount)).await;
    };

    let sender_balance = match state.casino_db.get_balance(sender_id) {
        Ok(Some(b)) => b, Ok(None) => return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await, Err(e) => {
            log::error!("casino get_balance failed for sender {sender_id}: {e}"); return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if sender_balance < amount {
        return reply(&bot, &msg, format!("Недостаточно средств: баланс {sender_balance}, нужно {amount}")).await;
    };

    if let Err(e) = state.casino_db.set_balance(sender_id, sender_balance - amount) { log::error!("casino set_balance failed for sender {sender_id}: {e}"); return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await; } if let Err(e) = state.casino_db.set_balance(recipient_id, recipient_balance + amount) { log::error!("casino set_balance failed for recipient {recipient_id}: {e}"); return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await; } reply(&bot, &msg, format!("Передано {amount}. Твой баланс: {}", sender_balance - amount)).await
}

async fn handle_man(bot: Bot, msg: Message) -> HandlerResult {
    reply(&bot, &msg, Command::descriptions().to_string()).await
}

async fn handle_ping(bot: Bot, msg: Message) -> HandlerResult {
    reply(&bot, &msg, "pong").await
}

async fn handle_id(bot: Bot, msg: Message) -> HandlerResult {
    let user_id = msg
        .from
        .as_ref()
        .map(|u| u.id.0.to_string())
        .unwrap_or_else(|| "unknown".to_string());
    reply(&bot, &msg, format!("Your id: {user_id}")).await
}

fn rate_string(input: &str) -> u32 {
    let sum: u32 = input.bytes().map(|b| b as u32).sum();
    sum % 101
}

async fn handle_rate(bot: Bot, msg: Message) -> HandlerResult {
    let Some(text) = extract_argument(&msg) else {
        return reply(&bot, &msg, "Напиши что оценивать: /rate <текст> или ответь на сообщение").await;
    };
    let score = rate_string(&text.to_lowercase());
    reply(&bot, &msg, format!("\"{text}\" --- {score}/100")).await
}

async fn handle_register(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;

    match state.casino_db.register(user_id, casino::STARTING_BALANCE) {
        Ok(true) => {
            reply(&bot, &msg, format!("Зарегистрирован! Баланс: {}", casino::STARTING_BALANCE)).await
        }
        Ok(false) => reply(&bot, &msg, "Ты уже зарегистрирован").await,
        Err(e) => {
            log::error!("casino register failed for {user_id}: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_balance(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;

    match state.casino_db.get_balance(user_id) {
        Ok(Some(balance)) => reply(&bot, &msg, format!("Твой баланс: {balance}")).await,
        Ok(None) => reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await,
        Err(e) => {
            log::error!("casino get_balance failed for {user_id}: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_game(bot: Bot, msg: Message, state: AppState, kind: GameKind) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;

    let balance = match state.casino_db.get_balance(user_id) {
        Ok(Some(b)) => b,
        Ok(None) => return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await,
        Err(e) => {
            log::error!("casino get_balance failed for {user_id}: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if balance <= 0 {
        return reply(&bot, &msg, "Баланс уже 0, играть не с чем").await;
    }

    let result = casino::play(kind, balance);

    if let Err(e) = state.casino_db.set_balance(user_id, result.new_balance) {
        log::error!("casino set_balance failed for {user_id}: {e}");
        return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
    }

    let outcome = if result.is_win { "Выигрыш!" } else { "Проигрыш." };
    reply(&bot, &msg, format!("{outcome} Новый баланс: {}", result.new_balance)).await
}

async fn handle_sent(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    if msg.chat.id != state.admin_chat_id {
        return reply(&bot, &msg, "Команда доступна только администратору").await;
    }

    let Some(command) = extract_argument(&msg) else {
        return reply(&bot, &msg, "Использование: /sent <команда>").await;
    };

    match state.shell_runner.execute(&command).await {
        Ok(result) => {
            let mut text = format!("exit code: {}", result.exit_code);
            if let Some(output) = result.output.filter(|s| !s.is_empty()) {
                text.push_str(&format!("\n\nstdout:\n{output}"));
            }
            if let Some(err) = result.err.filter(|s| !s.is_empty()) {
                text.push_str(&format!("\n\nstderr:\n{err}"));
            }
            reply(&bot, &msg, text).await
        }
        Err(e) => {
            log::error!("shell_runner execute failed: {e}");
            reply(&bot, &msg, "shell-runner недоступен или вернул ошибку").await
        }
    }
}
