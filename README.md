# 🚗 CarInsuranceBot

CarInsuranceBot is a Telegram bot that automates car insurance registration using a step-by-step workflow. It leverages OCR (Mindee) for document data extraction and OpenAI for generating custom responses such as the insurance policy number.

---

[![Bot work demonstration](https://img.youtube.com/vi/i3q1rj_I_7Y/0.jpg)](https://www.youtube.com/watch?v=i3q1rj_I_7Y)

## 🛠️ Tech Stack

- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- ASP.NET Core Web API
- Entity Framework Core (SQLite)
- Telegram Bot API (via `Telegram.Bot`)
- OpenAI API
- Mindee API (OCR)
- MediatR, AutoMapper, FluentValidation
- Serilog (Console & File logging)
- Hosted on [Render](https://render.com)

---

## 📦 NuGet Packages

### `CarInsuranceBot.Api`
- `Microsoft.AspNetCore.OpenApi`
- `Swashbuckle.AspNetCore`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`
- `Microsoft.EntityFrameworkCore.Design`

### `CarInsuranceBot.Core`
- `System.ComponentModel.Annotations`
- `FluentValidation`

### `CarInsuranceBot.Infrastructure`
- `Telegram.Bot`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Tools`
- `Newtonsoft.Json`
- `iTextSharp.LGPLv2.Core`
- `RestSharp`
- `Microsoft.Extensions.Http`
- `Microsoft.Extensions.Caching.Memory`

### `CarInsuranceBot.Application`
- `MediatR`
- `AutoMapper`
- `FluentValidation.DependencyInjectionExtensions`

---

## 🧱 Clean Architecture Rules

To ensure a maintainable and scalable architecture, the following dependency rules **must be respected**:

### ❌ Forbidden Dependencies

- `Core` **must NOT depend** on `Infrastructure`, `Application`, or `Api`
- `Infrastructure` **must NOT depend** on `Application` or `Api`
- `Application` **must NOT depend** on `Api`

### ✅ Allowed Dependencies

- All layers **can depend** on `Core`
- `Application` **can depend** on `Infrastructure`
- `Api` **can depend** on all layers

These constraints enforce a clean separation of concerns and allow for better testability and flexibility.

---

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- Telegram bot token from [@BotFather](https://t.me/BotFather)
- Mindee API keys (for front and back of vehicle registration doc)
- OpenAI API key

---

### ⚙️ Environment Configuration

On Render, set the following environment variables under "Environment → Secret Files" or as individual variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bot.db"
  },
  "Telegram": {
    "Token": "YOUR_TELEGRAM_BOT_TOKEN"
  },
  "Mindee": {
    "ApiKeyFront": "YOUR_MINDEE_API_KEY_FRONT",
    "ApiKeyBack": "YOUR_MINDEE_API_KEY_BACK"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  }
}


