
# RuzenBot

<img width="200" height="200" alt="image" src="https://github.com/user-attachments/assets/6ca5dd06-30b5-4ac4-9833-87ec880721b7" />

RuzenBot is a **modular Telegram bot** written in **C# (.NET)**, featuring microservice architecture, GitHub API integration, command execution, and a built-in casino system.
It’s designed for flexibility, easy extension, and seamless deployment via Docker.

---

## 📖 Table of Contents

* [Overview](#overview)
* [Features](#features)
* [Technologies](#technologies)
* [Installation & Setup](#installation--setup)
* [Environment Variables](#environment-variables)
* [Available Commands](#available-commands)
* [Project Structure](#project-structure)
* [Development](#development)
* [License](#license)

---

## 🧩 Overview

RuzenBot is a **multi-service Telegram bot** that uses **Dependency Injection (DI)**, **Hosted Services**, and **structured logging** to handle various Telegram events.
It can run in:

* **Microservice mode** (default) — enables modules such as GitHub API, Casino, and Shell execution.
* **Standalone mode** — simpler mode for testing, disabled via `--no-microservices`.

### Core Components

| Module                   | Description                                                     |
| ------------------------ | --------------------------------------------------------------- |
| **CommandService**       | Processes bot commands (`/ping`, `/gitrepo`, `/balance`, etc.). |
| **MessageHandler**       | Handles regular incoming text messages.                         |
| **CallbackQueryHandler** | Manages inline button interactions.                             |
| **GithubApiService**     | Retrieves GitHub user and repository data.                      |
| **CasinoService**        | Mini casino system with user registration and balance tracking. |
| **ShellRunnerService**   | Executes shell commands (with timeout and safe handling).       |
| **BotDbService**         | Manages data persistence.                                       |
| **Logger**               | Built with **NeoSimpleLogger** for clean, contextual logs.      |

---

## ✨ Features

* ⚙️ Modular design with dependency injection
* 🔁 Microservices support (optional CLI flag)
* 💬 Full Telegram API integration
* 🎰 Casino subsystem with virtual balance and registration
* 🧠 GitHub API data fetching (`/gituser`, `/gitrepo`)
* 💻 Shell command execution from Telegram (`/sent`)
* 🚨 Message reporting to admin chat (`/report`)
* 🧩 Custom command handler with easy extensibility
* 🪵 Structured logging via **NeoSimpleLogger**

---

## 🧰 Technologies

* **C# / .NET 8**
* **Telegram.Bot**
* **Microsoft.Extensions.Hosting**
* **Microsoft.Extensions.DependencyInjection**
* **CommandLineParser**
* **NeoSimpleLogger**
* **Docker / Docker Compose**

---

## ⚙️ Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/ruzen42/ruzenbot.git
cd ruzenbot
```

### 2. Set Environment Variables

Create a `.env` file or export them manually:

```env
TOKEN=your_telegram_bot_token
```

### 3. Run with Docker

```bash
docker-compose up --build
```

The bot will automatically start inside a Docker container.

### 4. Run Locally (without Docker)

```bash
dotnet run --project RuzenBot
```

Or run in standalone mode (no microservices):

```bash
dotnet run --project RuzenBot -- --no-microservices
```

---

## 🌍 Environment Variables

| Variable | Description                             | Required |
| -------- | --------------------------------------- | -------- |
| `TOKEN`  | Your Telegram bot token from @BotFather | ✅ Yes    |

---

## 💬 Available Commands

Below is the list of built-in commands handled by the `CommandService`:

| Command     | Description                                         | Example                        |
| ----------- | --------------------------------------------------- | ------------------------------ |
| `/man`      | Show help and list all commands                     | `/man`                         |
| `/register` | Register a casino account (starts with 100 credits) | `/register`                    |
| `/balance`  | Show your casino balance                            | `/balance`                     |
| `/rate`     | Rate a word or message between 0–100                | `/rate pizza` or reply `/rate` |
| `/ping`     | Health check command (“Pong”)                       | `/ping`                        |
| `/sent`     | Execute a shell command (admin only)                | `/sent ls -l`                  |
| `/id`       | Get your Telegram user ID                           | `/id`                          |
| `/report`   | Report a message to the admin (reply required)      | Reply `/report spam`           |
| `/gitrepo`  | Get information about a GitHub repository           | `/gitrepo user/repo`           |
| `/gituser`  | Get information about a GitHub user                 | `/gituser username`            |

> 💡 **Note:** Some commands (like `/sent` and `/report`) may be restricted to admin users for security reasons.

---

## 🗂️ Project Structure

```
RuzenBot/
├── Program.cs                # Application entry point and host setup
├── Services/
│   ├── CommandService.cs      # Telegram command handling
│   ├── Casino/               # Casino-related logic
│   ├── GithubApi/            # GitHub API integration
│   ├── ShellRunner/          # Safe shell execution
│   ├── Message/              # Telegram message processing
│   ├── CallbackQuery/        # Inline callback handling
│   ├── Update/               # Update polling service
│   └── Bot/                  # Telegram bot orchestration
├── docker-compose.yml        # Docker configuration
└── README.md                 # Documentation
```

---

## 🧑‍💻 Development

You’ll need **.NET 8 SDK** or later.

```bash
dotnet restore
dotnet build
dotnet run
```

Logs are displayed in the console via **NeoSimpleLogger**.

---

## 🪪 License

RuzenBot is distributed under the **GPLv3 License**.
Created by [ruzen42](https://github.com/ruzen42).

