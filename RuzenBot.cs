using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;


public class RuzenBot
{
    private static ITelegramBotClient _client = null ;
    private static ReceiverOptions _receiverOptions = null;

    private static readonly string[] Answers = new[]
    {
        "Бесспорно!",
        "Да, но будь осторожен.",
        "Сомнительно...",
        "Ни в коем случае!",
        "Спроси позже.",
        "Мой ответ — нет.",
        "Знаки говорят «да»!",
        "Лучше не рассказывать.",
        "Даже не думай!",
        "Абсолютно точно!",
        "Абсолютно точно!"
    };
    
    static async Task Main(string[] args)
    {
        //var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory()));
        //DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { Path.Combine(projectRoot, ".env") }));
        var token = Environment.GetEnvironmentVariable("SECRET");
        if (string.IsNullOrWhiteSpace(token))
            Environment.Exit(1488);
        try
        {
            _client = new TelegramBotClient(token);
        }
        catch 
        {
            Console.WriteLine("please export SECRET=token");
            Environment.Exit(1);
        }

    
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },

            DropPendingUpdates = true,
        };
        
        using var cts = new CancellationTokenSource();
        
        _client.StartReceiving(UpdateHandler, ErrorEventHandler, _receiverOptions, cts.Token);

        var me = await _client.GetMeAsync();
        Console.WriteLine($"Hello {me.FirstName} {me.LastName}!");
        await Task.Delay(-1, cts.Token);
    }

    private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken cancellationTokenA)
    {
        var messageParts = update.Message.Text.Split(' ', 2);
        var command = messageParts[0];
        var arguments = messageParts.Length > 1 ? messageParts[1] : null;
        
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    var message = update.Message;
                    var user = message.From;
                    var chat = message.Chat;
                    
                    switch (command)
                    {
                        case ("/start"):
                        {
                            await bot.SendTextMessageAsync(
                                chat.Id,
                                "Start" +
                                "/inline\n" +
                                "/reply\n");
                            return;
                        }
                        case ("/inline"):
                        {
                            var inclineKeyboard = new InlineKeyboardMarkup(
                                new List<InlineKeyboardButton[]>()
                                {
                                    new InlineKeyboardButton[]
                                    {
                                        InlineKeyboardButton.WithUrl("ТГК", "https://t.me/+T54_jNmiS7plNDdi"),
                                        InlineKeyboardButton.WithCallbackData("Просто кнопочка", "button1"),
                                    },
                                    new InlineKeyboardButton[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("кнопочка", "button2"),
                                        InlineKeyboardButton.WithCallbackData("и еще одна кнопочка", "button3"),
                                    },
                                });

                            await bot.SendTextMessageAsync(
                                chat.Id,
                                "это inline клава",
                                replyMarkup: inclineKeyboard);

                            return;
                        }
                            case ("/myid"):
                            {
                                await bot.SendTextMessageAsync(
                                    chat.Id,
                                    "Вы настолько еблан что ваш ID это " + user.Id,
                                    replyParameters: new ReplyParameters
                                    {
                                        MessageId = message.MessageId,
                                    }
                                );
                                break;
                            }
                                
                            case ("/whoami"):
                            { 
                                Console.WriteLine("help");
                                await bot.SendTextMessageAsync(
                                    chat.Id,
                                    user.FirstName + " " + user.LastName,
                                    replyParameters: new ReplyParameters
                                    {
                                        MessageId = message.MessageId,
                                    });
                                return;
                            }
                            
                            case ("/8ball"):
                            {
                                Console.WriteLine("help");
                                var random = new Random();
                                var answer = Answers[random.Next(Answers.Length)]; 
                                
                                await bot.SendTextMessageAsync(
                                    chat.Id,
                                    answer,
                                    replyParameters: new ReplyParameters
                                    {
                                        MessageId = message.MessageId,
                                    });
                                return;
                            }

                        case ("/bash"):
                        {
                            Console.WriteLine(chat);
                            await bot.SendTextMessageAsync(
                                chat.Id,
                                 BashCall(arguments),
                                replyParameters: new ReplyParameters
                                {
                                    MessageId = message.MessageId,
                                });
                            Environment.Exit(2);
                            return; 
                        }

                        case ("/help"):
                        {
                            Console.WriteLine("help");
                            await bot.SendTextMessageAsync(
                                chat.Id,
                                "Правила чата\n\n\tстрельба в хохлов разрешена\n\nбольше правил нет",
                                replyParameters: new ReplyParameters
                                {
                                    MessageId = message.MessageId,
                                });
                            return;
                        }
                            
                    }
                } break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Environment.Exit(3);
        }
    }

    private static Task ErrorEventHandler(ITelegramBotClient bot, Exception error, CancellationToken cancellationTokenA)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException => apiRequestException.Message,
        };
        
        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    private static string BashCall(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/zsh",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
            }
        };
        
        process.Start();

        string result = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return error;
        }
        
        if (result.Length < 1000 && !string.IsNullOrWhiteSpace(result)) 
        {
            return result;
        }
        
        return "Слишком большой вывод, терминация процесса...";
    }
}
