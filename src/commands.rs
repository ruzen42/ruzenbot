use crate::casino::{self, GameKind};
use crate::state::{AppState, DuelChallenge};
use teloxide::prelude::*;
use teloxide::types::{CallbackQuery, ChatId, InlineKeyboardButton, InlineKeyboardMarkup, ReplyParameters};
use teloxide::utils::command::BotCommands;

pub type HandlerResult = Result<(), teloxide::RequestError>;

#[derive(BotCommands, Clone)]
#[command(rename_rule = "lowercase", description = "Доступные команды:")]
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
    #[command(description = "сыграть в 'двадцать на восемьдесят' (x1.1 / x0.2)")]
    TwentyEighteen,
    #[command(description = "подбросить монетку на ставку: /coinflip <сумма>")]
    Coinflip,
    #[command(description = "вызвать на дуэль ответом на сообщение: /duel <ставка>")]
    Duel,
    #[command(description = "выполнить shell-команду через shell-runner (только админ)")]
    Sent,
    #[command(description = "передать деньги тому, кому отвечаешь: /give <сумма>")]
    Give,
    #[command(description = "купить +1% к ежедневному бусту за 10000")]
    Boost,
    #[command(description = "сбросить свой буст обратно к 10%")]
    ResetBoost,
    #[command(description = "топ 10 игроков по балансу")]
    Top,
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

fn display_name(user: &teloxide::types::User) -> String {
    user.username.clone().unwrap_or_else(|| user.first_name.clone())
}

async fn reply(bot: &Bot, msg: &Message, text: impl Into<String>) -> HandlerResult {
    bot.send_message(msg.chat.id, text)
        .reply_parameters(ReplyParameters::new(msg.id))
        .await?;
    Ok(())
}

pub async fn dispatch(bot: Bot, msg: Message, cmd: Command, state: AppState) -> HandlerResult {
    let user_id = msg.from.as_ref().map(|u| u.id.0).unwrap_or(0);
    log::info!("command received: user={user_id} chat={} cmd={:?}", msg.chat.id.0, cmd_name(&cmd));

    let result = match cmd {
        Command::Man => handle_man(bot, msg).await,
        Command::Ping => handle_ping(bot, msg).await,
        Command::Id => handle_id(bot, msg).await,
        Command::Rate => handle_rate(bot, msg).await,
        Command::Register => handle_register(bot, msg, state).await,
        Command::Balance => handle_balance(bot, msg, state).await,
        Command::AllOrNothing => handle_game(bot, msg, state, GameKind::AllOrNothing).await,
        Command::FiftyFifty => handle_game(bot, msg, state, GameKind::FiftyFifty).await,
        Command::TwentyEighteen => handle_game(bot, msg, state, GameKind::TwentyEighteen).await,
        Command::Coinflip => handle_coinflip(bot, msg, state).await,
        Command::Duel => handle_duel(bot, msg, state).await,
        Command::Sent => handle_sent(bot, msg, state).await,
        Command::Give => handle_give(bot, msg, state).await,
        Command::Boost => handle_boost(bot, msg, state).await,
        Command::ResetBoost => handle_reset_boost(bot, msg, state).await,
        Command::Top => handle_top(bot, msg, state).await,
    };

    match &result {
        Ok(()) => log::info!("command handled successfully: user={user_id}"),
        Err(e) => log::error!("command handling failed: user={user_id} error={e}"),
    }

    result
}

fn cmd_name(cmd: &Command) -> &'static str {
    match cmd {
        Command::Man => "man",
        Command::Ping => "ping",
        Command::Id => "id",
        Command::Rate => "rate",
        Command::Register => "register",
        Command::Balance => "balance",
        Command::AllOrNothing => "allornothing",
        Command::FiftyFifty => "fiftyfifty",
        Command::TwentyEighteen => "twentyeighteen",
        Command::Coinflip => "coinflip",
        Command::Duel => "duel",
        Command::Sent => "sent",
        Command::Give => "give",
        Command::Boost => "boost",
        Command::ResetBoost => "reset_boost",
        Command::Top => "top",
    }
}

async fn handle_man(bot: Bot, msg: Message) -> HandlerResult {
    log::debug!("handle_man: chat={}", msg.chat.id.0);
    reply(&bot, &msg, Command::descriptions().to_string()).await
}

async fn handle_ping(bot: Bot, msg: Message) -> HandlerResult {
    log::debug!("handle_ping: chat={}", msg.chat.id.0);
    reply(&bot, &msg, "pong").await
}

async fn handle_id(bot: Bot, msg: Message) -> HandlerResult {
    let user_id = msg.from.as_ref().map(|u| u.id.0.to_string()).unwrap_or_else(|| "unknown".to_string());
    log::debug!("handle_id: user={user_id}");
    reply(&bot, &msg, format!("Your id: {user_id}")).await
}

fn rate_string(input: &str) -> u32 {
    let sum: u32 = input.bytes().map(|b| b as u32).sum();
    sum % 101
}

async fn handle_rate(bot: Bot, msg: Message) -> HandlerResult {
    let Some(text) = extract_argument(&msg) else {
        log::debug!("handle_rate: no argument provided, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Напиши что оценивать: /rate <текст> или ответь на сообщение").await;
    };
    let score = rate_string(&text.to_lowercase());
    log::info!("handle_rate: text={text:?} score={score}");
    reply(&bot, &msg, format!("\"{text}\" --- {score}/100")).await
}

async fn handle_register(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_register: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;
    log::info!("handle_register: user={user_id}");

    match state.casino_db.register(user_id, casino::STARTING_BALANCE) {
        Ok(true) => {
            log::info!("handle_register: user={user_id} registered successfully");
            reply(&bot, &msg, format!("Зарегистрирован! Баланс: {}", casino::STARTING_BALANCE)).await
        }
        Ok(false) => {
            log::info!("handle_register: user={user_id} already registered");
            reply(&bot, &msg, "Ты уже зарегистрирован").await
        }
        Err(e) => {
            log::error!("handle_register: user={user_id} db error: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_balance(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_balance: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;
    log::info!("handle_balance: user={user_id}");

    match state.casino_db.get_balance(user_id) {
        Ok(Some(balance)) => {
            log::info!("handle_balance: user={user_id} balance={balance}");
            let boost = state.casino_db.get_boost_percent(user_id).ok().flatten().unwrap_or(10);
            reply(&bot, &msg, format!("Твой баланс: {balance} (буст: +{boost}%/день)")).await
        }
        Ok(None) => {
            log::info!("handle_balance: user={user_id} not registered");
            reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await
        }
        Err(e) => {
            log::error!("handle_balance: user={user_id} db error: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_game(bot: Bot, msg: Message, state: AppState, kind: GameKind) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_game: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;
    log::info!("handle_game: user={user_id}");

    let balance = match state.casino_db.get_balance(user_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_game: user={user_id} not registered");
            return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await;
        }
        Err(e) => {
            log::error!("handle_game: user={user_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if balance <= 0 {
        log::info!("handle_game: user={user_id} has zero balance");
        return reply(&bot, &msg, "Баланс уже 0, играть не с чем").await;
    }

    let result = casino::play(kind, balance);
    log::info!("handle_game: user={user_id} is_win={} new_balance={}", result.is_win, result.new_balance);

    if let Err(e) = state.casino_db.set_balance(user_id, result.new_balance) {
        log::error!("handle_game: user={user_id} set_balance failed: {e}");
        return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
    }

    let outcome = if result.is_win { "Выигрыш!" } else { "Проигрыш." };
    reply(&bot, &msg, format!("{outcome} Новый баланс: {}", result.new_balance)).await
}

async fn handle_coinflip(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_coinflip: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;

    let Some(bet_str) = extract_argument(&msg) else {
        log::debug!("handle_coinflip: no bet provided, user={user_id}");
        return reply(&bot, &msg, "Использование: /coinflip <сумма>").await;
    };
    let Ok(bet) = bet_str.parse::<i64>() else {
        log::debug!("handle_coinflip: invalid bet {bet_str:?}, user={user_id}");
        return reply(&bot, &msg, "Ставка должна быть целым положительным числом").await;
    };
    if bet <= 0 {
        log::debug!("handle_coinflip: non-positive bet {bet}, user={user_id}");
        return reply(&bot, &msg, "Ставка должна быть положительной").await;
    }

    log::info!("handle_coinflip: user={user_id} bet={bet}");

    let balance = match state.casino_db.get_balance(user_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_coinflip: user={user_id} not registered");
            return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await;
        }
        Err(e) => {
            log::error!("handle_coinflip: user={user_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if balance < bet {
        log::info!("handle_coinflip: user={user_id} insufficient balance={balance} bet={bet}");
        return reply(&bot, &msg, format!("Недостаточно средств: баланс {balance}, ставка {bet}")).await;
    }

    let result = casino::play(GameKind::Coinflip(bet), balance);
    log::info!("handle_coinflip: user={user_id} is_win={} new_balance={}", result.is_win, result.new_balance);

    if let Err(e) = state.casino_db.set_balance(user_id, result.new_balance) {
        log::error!("handle_coinflip: user={user_id} set_balance failed: {e}");
        return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
    }

    let outcome = if result.is_win { format!("Орёл! +{bet}") } else { format!("Решка! -{bet}") };
    reply(&bot, &msg, format!("{outcome}. Новый баланс: {}", result.new_balance)).await
}

async fn handle_duel(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(challenger) = msg.from.as_ref() else {
        log::warn!("handle_duel: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить отправителя").await;
    };
    let challenger_id = challenger.id.0;
    let challenger_name = display_name(challenger);

    let Some(target_msg) = msg.reply_to_message() else {
        log::debug!("handle_duel: no reply target, user={challenger_id}");
        return reply(&bot, &msg, "Ответь на сообщение того, кого вызываешь: /duel <ставка>").await;
    };
    let Some(opponent) = target_msg.from.as_ref() else {
        log::warn!("handle_duel: no opponent info, user={challenger_id}");
        return reply(&bot, &msg, "Не удалось определить оппонента").await;
    };
    let opponent_id = opponent.id.0;
    let opponent_name = display_name(opponent);

    if opponent_id == challenger_id {
        log::debug!("handle_duel: self-challenge attempt, user={challenger_id}");
        return reply(&bot, &msg, "Нельзя вызвать на дуэль самого себя").await;
    }

    let Some(wager_str) = extract_argument(&msg) else {
        log::debug!("handle_duel: no wager provided, user={challenger_id}");
        return reply(&bot, &msg, "Использование: /duel <ставка> (ответом на сообщение оппонента)").await;
    };
    let Ok(wager) = wager_str.parse::<i64>() else {
        log::debug!("handle_duel: invalid wager {wager_str:?}, user={challenger_id}");
        return reply(&bot, &msg, "Ставка должна быть целым положительным числом").await;
    };
    if wager <= 0 {
        log::debug!("handle_duel: non-positive wager {wager}, user={challenger_id}");
        return reply(&bot, &msg, "Ставка должна быть положительной").await;
    }

    log::info!("handle_duel: challenger={challenger_id} opponent={opponent_id} wager={wager}");

    let challenger_balance = match state.casino_db.get_balance(challenger_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_duel: challenger={challenger_id} not registered");
            return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await;
        }
        Err(e) => {
            log::error!("handle_duel: challenger={challenger_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };
    if challenger_balance < wager {
        log::info!("handle_duel: challenger={challenger_id} insufficient balance={challenger_balance} wager={wager}");
        return reply(&bot, &msg, format!("Недостаточно средств: баланс {challenger_balance}, ставка {wager}")).await;
    }

    let opponent_balance = match state.casino_db.get_balance(opponent_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_duel: opponent={opponent_id} not registered");
            return reply(&bot, &msg, "Оппонент ещё не зарегистрирован (/register)").await;
        }
        Err(e) => {
            log::error!("handle_duel: opponent={opponent_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };
    if opponent_balance < wager {
        log::info!("handle_duel: opponent={opponent_id} insufficient balance={opponent_balance} wager={wager}");
        return reply(&bot, &msg, format!("У оппонента недостаточно средств (баланс {opponent_balance}, нужна ставка {wager})")).await;
    }

    let token = state.next_duel_token();
    log::info!("handle_duel: challenge created token={token} challenger={challenger_id} opponent={opponent_id} wager={wager}");

    state.duel_challenges.lock().unwrap().insert(
        token,
        DuelChallenge {
            challenger_id,
            challenger_name: challenger_name.clone(),
            opponent_id,
            opponent_name: opponent_name.clone(),
            wager,
        },
    );

    let keyboard = InlineKeyboardMarkup::new(vec![vec![
        InlineKeyboardButton::callback("Принять", format!("duel_accept:{token}")),
        InlineKeyboardButton::callback("Отклонить", format!("duel_decline:{token}")),
    ]]);

    bot.send_message(
        msg.chat.id,
        format!("{challenger_name} вызывает {opponent_name} на дуэль на {wager}!"),
    )
    .reply_markup(keyboard)
    .await?;

    Ok(())
}

pub async fn handle_duel_callback(bot: Bot, query: CallbackQuery, state: AppState) -> HandlerResult {
    let Some(data) = query.data.as_deref() else {
        log::warn!("handle_duel_callback: no data in callback query");
        return Ok(());
    };
    log::info!("handle_duel_callback: data={data} from_user={}", query.from.id.0);

    let Some(message) = query.message.as_ref() else {
        log::warn!("handle_duel_callback: no message in callback query");
        return Ok(());
    };

    let (action, token_str) = match data.split_once(':') {
        Some(pair) => pair,
        None => {
            log::warn!("handle_duel_callback: malformed callback data={data}");
            return Ok(());
        }
    };
    let Ok(token) = token_str.parse::<u64>() else {
        log::warn!("handle_duel_callback: invalid token in data={data}");
        return Ok(());
    };

    let challenge = state.duel_challenges.lock().unwrap().remove(&token);
    let Some(challenge) = challenge else {
        log::info!("handle_duel_callback: token={token} not found (already resolved or expired)");
        bot.answer_callback_query(query.id.clone())
            .text("Эта дуэль уже неактуальна")
            .await?;
        return Ok(());
    };

    if query.from.id.0 != challenge.opponent_id {
        log::info!("handle_duel_callback: token={token} wrong responder={}", query.from.id.0);
        state.duel_challenges.lock().unwrap().insert(token, challenge);
        bot.answer_callback_query(query.id.clone())
            .text("Это не твой вызов")
            .await?;
        return Ok(());
    }

    if action == "duel_decline" {
        log::info!("handle_duel_callback: token={token} declined by opponent={}", challenge.opponent_id);
        bot.edit_message_text(
            message.chat().id,
            message.id(),
            format!("{} отклонил дуэль от {}", challenge.opponent_name, challenge.challenger_name),
        )
        .await?;
        bot.answer_callback_query(query.id.clone()).await?;
        return Ok(());
    }

    log::info!("handle_duel_callback: token={token} accepted, resolving duel");

    let challenger_balance = match state.casino_db.get_balance(challenge.challenger_id) {
        Ok(Some(b)) => b,
        _ => {
            log::error!("handle_duel_callback: token={token} challenger balance unavailable at resolve time");
            bot.answer_callback_query(query.id.clone())
                .text("Ошибка: баланс вызывающего недоступен")
                .await?;
            return Ok(());
        }
    };
    let opponent_balance = match state.casino_db.get_balance(challenge.opponent_id) {
        Ok(Some(b)) => b,
        _ => {
            log::error!("handle_duel_callback: token={token} opponent balance unavailable at resolve time");
            bot.answer_callback_query(query.id.clone())
                .text("Ошибка: баланс оппонента недоступен")
                .await?;
            return Ok(());
        }
    };

    if challenger_balance < challenge.wager || opponent_balance < challenge.wager {
        log::info!("handle_duel_callback: token={token} insufficient balance at resolve time");
        bot.edit_message_text(
            message.chat().id,
            message.id(),
            "Дуэль отменена: у одного из участников не хватает средств на момент подтверждения",
        )
        .await?;
        bot.answer_callback_query(query.id.clone()).await?;
        return Ok(());
    }

    let challenger_wins = casino::resolve_duel(challenger_balance, opponent_balance, challenge.wager);
    let (winner_id, loser_id, winner_name, loser_name) = if challenger_wins {
        (challenge.challenger_id, challenge.opponent_id, challenge.challenger_name.clone(), challenge.opponent_name.clone())
    } else {
        (challenge.opponent_id, challenge.challenger_id, challenge.opponent_name.clone(), challenge.challenger_name.clone())
    };

    log::info!("handle_duel_callback: token={token} winner={winner_id} loser={loser_id} wager={}", challenge.wager);

    if let Err(e) = state.casino_db.transfer(loser_id, winner_id, challenge.wager) {
        log::error!("handle_duel_callback: token={token} transfer failed: {e}");
        bot.answer_callback_query(query.id.clone())
            .text("Ошибка базы данных при выплате")
            .await?;
        return Ok(());
    }

    bot.edit_message_text(
        message.chat().id,
        message.id(),
        format!("{winner_name} побеждает {loser_name} в дуэли и забирает {}!", challenge.wager),
    )
    .await?;
    bot.answer_callback_query(query.id.clone()).await?;

    Ok(())
}

async fn handle_give(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(sender) = msg.from.as_ref() else {
        log::warn!("handle_give: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить отправителя").await;
    };
    let sender_id = sender.id.0;

    let Some(target_msg) = msg.reply_to_message() else {
        log::debug!("handle_give: no reply target, user={sender_id}");
        return reply(&bot, &msg, "Ответь на сообщение того, кому хочешь передать деньги: /give <сумма>").await;
    };
    let Some(recipient) = target_msg.from.as_ref() else {
        log::warn!("handle_give: no recipient info, user={sender_id}");
        return reply(&bot, &msg, "Не удалось определить получателя").await;
    };
    let recipient_id = recipient.id.0;

    if recipient_id == sender_id {
        log::debug!("handle_give: self-give attempt, user={sender_id}");
        return reply(&bot, &msg, "Себе передавать деньги нельзя").await;
    }

    let is_admin = sender_id == state.admin_chat_id.0 as u64;

    let Some(amount_str) = msg.text().and_then(|t| t.split_once(char::is_whitespace)).map(|(_, rest)| rest.trim()) else {
        log::debug!("handle_give: no amount provided, user={sender_id}");
        return reply(&bot, &msg, "Использование: /give <сумма> (ответом на сообщение получателя)").await;
    };
    let Ok(amount) = amount_str.parse::<i64>() else {
        log::debug!("handle_give: invalid amount {amount_str:?}, user={sender_id}");
        return reply(&bot, &msg, "Сумма должна быть целым положительным числом").await;
    };
    if amount <= 0 {
        log::debug!("handle_give: non-positive amount {amount}, user={sender_id}");
        return reply(&bot, &msg, "Сумма должна быть положительной").await;
    }

    log::info!("handle_give: sender={sender_id} recipient={recipient_id} amount={amount} is_admin={is_admin}");

    let recipient_balance = match state.casino_db.get_balance(recipient_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_give: recipient={recipient_id} not registered");
            return reply(&bot, &msg, "Получатель ещё не зарегистрирован (/register)").await;
        }
        Err(e) => {
            log::error!("handle_give: recipient={recipient_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if is_admin {
        if let Err(e) = state.casino_db.set_balance(recipient_id, recipient_balance + amount) {
            log::error!("handle_give: admin grant to recipient={recipient_id} failed: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
        log::info!("handle_give: admin={sender_id} granted {amount} to recipient={recipient_id}");
        return reply(&bot, &msg, format!("Админ выдал {amount} пользователю. Новый баланс получателя: {}", recipient_balance + amount)).await;
    }

    let sender_balance = match state.casino_db.get_balance(sender_id) {
        Ok(Some(b)) => b,
        Ok(None) => {
            log::info!("handle_give: sender={sender_id} not registered");
            return reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await;
        }
        Err(e) => {
            log::error!("handle_give: sender={sender_id} db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if sender_balance < amount {
        log::info!("handle_give: sender={sender_id} insufficient balance={sender_balance} amount={amount}");
        return reply(&bot, &msg, format!("Недостаточно средств: баланс {sender_balance}, нужно {amount}")).await;
    }

    if let Err(e) = state.casino_db.transfer(sender_id, recipient_id, amount) {
        log::error!("handle_give: transfer sender={sender_id} recipient={recipient_id} failed: {e}");
        return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
    }

    log::info!("handle_give: transfer complete sender={sender_id} recipient={recipient_id} amount={amount}");
    reply(&bot, &msg, format!("Передано {amount}. Твой баланс: {}", sender_balance - amount)).await
}

async fn handle_boost(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_boost: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;
    log::info!("handle_boost: user={user_id}");

    match state.casino_db.buy_boost(user_id) {
        Ok(casino::BuyBoostOutcome::Purchased { new_balance, new_boost_percent }) => {
            log::info!("handle_boost: user={user_id} purchased boost={new_boost_percent} balance={new_balance}");
            reply(
                &bot,
                &msg,
                format!(
                    "Буст прокачан до {new_boost_percent}%! Списано {}. Баланс: {new_balance}",
                    casino::BOOST_COST
                ),
            )
            .await
        }
        Ok(casino::BuyBoostOutcome::InsufficientFunds { balance }) => {
            log::info!("handle_boost: user={user_id} insufficient funds balance={balance}");
            reply(
                &bot,
                &msg,
                format!("Недостаточно средств: нужно {}, у тебя {balance}", casino::BOOST_COST),
            )
            .await
        }
        Err(casino::CasinoDbError::NotRegistered(_)) => {
            log::info!("handle_boost: user={user_id} not registered");
            reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await
        }
        Err(e) => {
            log::error!("handle_boost: user={user_id} db error: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_reset_boost(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    let Some(user) = msg.from.as_ref() else {
        log::warn!("handle_reset_boost: no sender info, chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Не удалось определить пользователя").await;
    };
    let user_id = user.id.0;
    log::info!("handle_reset_boost: user={user_id}");

    match state.casino_db.reset_boost(user_id) {
        Ok(true) => {
            log::info!("handle_reset_boost: user={user_id} reset to default");
            reply(&bot, &msg, "Буст сброшен до 10%").await
        }
        Ok(false) => {
            log::info!("handle_reset_boost: user={user_id} not registered");
            reply(&bot, &msg, "Ты не зарегистрирован, используй /register").await
        }
        Err(e) => {
            log::error!("handle_reset_boost: user={user_id} db error: {e}");
            reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await
        }
    }
}

async fn handle_top(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    log::info!("handle_top: chat={}", msg.chat.id.0);

    let entries = match state.casino_db.top_players(10) {
        Ok(e) => e,
        Err(e) => {
            log::error!("handle_top: db error: {e}");
            return reply(&bot, &msg, "Ошибка базы данных, попробуй позже").await;
        }
    };

    if entries.is_empty() {
        log::info!("handle_top: no registered players yet");
        return reply(&bot, &msg, "Пока никто не зарегистрирован в казино").await;
    }

    let mut lines = Vec::with_capacity(entries.len());
    for (i, (user_id, balance)) in entries.iter().enumerate() {
        let name = match bot.get_chat(ChatId(*user_id as i64)).await {
            Ok(chat) => chat
                .username()
                .map(|u| format!("@{u}"))
                .or_else(|| chat.first_name().map(|n| n.to_string()))
                .unwrap_or_else(|| user_id.to_string()),
            Err(e) => {
                log::debug!("handle_top: get_chat failed for user={user_id}: {e}");
                user_id.to_string()
            }
        };
        lines.push(format!("{}. {name} — {balance}", i + 1));
    }

    log::info!("handle_top: rendered {} entries", entries.len());
    reply(&bot, &msg, format!("Топ игроков:\n{}", lines.join("\n"))).await
}

async fn handle_sent(bot: Bot, msg: Message, state: AppState) -> HandlerResult {
    if msg.chat.id != state.admin_chat_id {
        log::warn!("handle_sent: unauthorized attempt from chat={}", msg.chat.id.0);
        return reply(&bot, &msg, "Команда доступна только администратору").await;
    }

    let Some(command) = extract_argument(&msg) else {
        log::debug!("handle_sent: no command provided");
        return reply(&bot, &msg, "Использование: /sent <команда>").await;
    };

    log::info!("handle_sent: executing command={command:?}");

    match state.shell_runner.execute(&command).await {
        Ok(result) => {
            log::info!("handle_sent: exit_code={} command={command:?}", result.exit_code);
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
            log::error!("handle_sent: shell_runner execute failed: {e}");
            reply(&bot, &msg, "shell-runner недоступен или вернул ошибку").await
        }
    }
}
