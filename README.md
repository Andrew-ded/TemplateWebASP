# TemplateWebASP

Шаблон для проектов ASP.NET с подготовленными предварительными настройками.

---

## Оглавление

1. [Структура решения](#структура-решения)
2. [Быстрый старт](#быстрый-старт)
3. [Архитектура](#архитектура)
4. [Конфигурация](#конфигурация)
5. [База данных (EF Core)](#база-данных-ef-core)
6. [API контроллеры и версионирование](#api-контроллеры-и-версионирование)
7. [MediatR и CQRS](#mediatr-и-cqrs)
8. [Аутентификация (JWT)](#аутентификация-jwt)
9. [Авторизация](#авторизация)
10. [API документация (Swagger + Scalar)](#api-документация-swagger--scalar)
11. [Rate Limiting](#rate-limiting)
12. [Логирование (Serilog)](#логирование-serilog)
13. [Razor Pages](#razor-pages)
14. [Стандартизированные ошибки API](#стандартизированные-ошибки-api)
15. [Как добавить новую сущность (пошагово)](#как-добавить-новую-сущность)
16. [NuGet пакеты](#nuget-пакеты)

---

## Структура решения

```
TemplateWebASP.sln
src/
├── Web/                          — Точка входа, контроллеры, Razor Pages, конфигурация
│   ├── Controllers/
│   │   ├── HealthController.cs   — Health-check эндпоинт
│   │   └── SampleController.cs   — Пример CRUD через MediatR
│   ├── Doc/
│   │   └── ConfigureSwaggerOptions.cs — Конфигурация Swagger (версии, JWT)
│   ├── Errors/
│   │   └── ApiErrors.cs          — Стандартизированный формат ошибок API
│   ├── Logging/
│   │   └── SourceContextFilter.cs — Фильтр Serilog для разделения логов
│   ├── Pages/
│   │   ├── Index.cshtml / .cs    — Главная страница
│   │   ├── Shared/_Layout.cshtml — Общий layout
│   │   └── _ViewImports.cshtml   — Импорты Tag Helpers
│   ├── wwwroot/css/site.css      — Базовые стили
│   ├── Program.cs                — Точка входа, вся конфигурация приложения
│   ├── appsettings.json          — Настройки (production)
│   └── appsettings.Development.json — Настройки (development, InMemory DB)
│
├── Application/                  — Бизнес-логика
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs — Pipeline: автовалидация через FluentValidation
│   │   └── LoggingBehavior.cs    — Pipeline: логирование запросов
│   ├── Features/
│   │   └── Sample/
│   │       ├── Commands/
│   │       │   ├── CreateSampleCommand.cs — Создание + валидатор
│   │       │   └── DeleteSampleCommand.cs — Удаление
│   │       └── Queries/
│   │           ├── GetAllSamplesQuery.cs   — Получить все
│   │           └── GetSampleByIdQuery.cs   — Получить по ID
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs — AddApplication()
│
├── Domain/                       — Доменные модели (без зависимостей)
│   ├── Entities/
│   │   ├── BaseEntity.cs         — Базовая сущность (Id)
│   │   └── SampleEntity.cs      — Пример сущности
│   └── Interfaces/
│       └── IRepository.cs        — Generic интерфейс репозитория
│
└── Infrastructure/               — Доступ к данным, конфигурация БД
    ├── DB/
    │   └── AppDbContext.cs        — DbContext с Fluent API
    ├── Options/
    │   ├── DatabaseOptions.cs    — Настройки подключения к БД
    │   └── JwtOptions.cs         — Настройки JWT
    ├── Repository/
    │   └── GenericRepository.cs  — Реализация IRepository<T>
    └── Extensions/
        └── ServiceCollectionExtensions.cs — AddInfrastructure()
```

**Зависимости между слоями:**

```
Web → Application → Infrastructure → Domain
```

`Domain` не зависит ни от кого. `Infrastructure` зависит только от `Domain`. `Application` зависит от `Domain` и `Infrastructure`. `Web` зависит от всех.

---

## Быстрый старт

```bash
cd src/Web
dotnet run
```

| URL | Описание |
|-----|----------|
| http://localhost:5000 | Главная (Razor Page) |
| http://localhost:5000/swagger | Swagger UI |
| http://localhost:5000/scalar/v1 | Scalar API Reference |
| http://localhost:5000/api/health | Health-check |
| http://localhost:5000/api/v1/sample | Sample API (требует JWT) |

В Development-режиме используется **InMemory база данных** — SQL Server не нужен.

---

## Архитектура

Проект построен на **Clean Architecture** с 4 слоями:

| Слой | Назначение | Зависимости |
|------|-----------|-------------|
| **Domain** | Сущности, интерфейсы. Чистый C#, без NuGet | Нет |
| **Infrastructure** | EF Core, репозитории, Options-классы | Domain |
| **Application** | MediatR handlers, валидация, pipeline behaviors | Domain, Infrastructure |
| **Web** | ASP.NET (контроллеры, Razor Pages, middleware конфигурация) | Все |

**Регистрация сервисов** вынесена в extension-методы:

```csharp
// Program.cs
builder.Services.AddInfrastructure(builder.Configuration); // DbContext, Repository, Options
builder.Services.AddApplication();                          // MediatR, Validators, Behaviors
```

---

## Конфигурация

### appsettings.json (production)

```json
{
  "Database": {
    "ConnectionString": "Data Source=...;Initial Catalog=...;",
    "UseInMemory": false
  },
  "Jwt": {
    "Issuer": "TemplateApp",
    "Audience": "TemplateApp.Client",
    "Key": "CHANGE_THIS_TO_A_LONG_RANDOM_JWT_SIGNING_KEY_MIN_32_CHARS",
    "AccessTokenMinutes": 60
  }
}
```

### appsettings.Development.json

```json
{
  "Database": {
    "ConnectionString": "",
    "UseInMemory": true
  },
  "Jwt": {
    "Key": "DevOnly_SuperSecretKey_1234567890_ChangeInProduction!",
    "AccessTokenMinutes": 120
  }
}
```

### Options Pattern (IOptions<T>)

Все настройки привязаны через `IOptions<T>` с **валидацией при старте**:

```csharp
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()  // Проверка [Required], [MinLength] и т.д.
    .ValidateOnStart();         // Ошибка при старте если конфиг невалиден
```

**Options-классы:**

| Класс | Секция | Расположение |
|-------|--------|-------------|
| `DatabaseOptions` | `Database` | `Infrastructure/Options/` |
| `JwtOptions` | `Jwt` | `Infrastructure/Options/` |

Пример Options-класса:

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required][MinLength(32)] public string Key { get; set; } = string.Empty;
    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 60;
}
```

### Как добавить новую секцию конфигурации

1. Создать Options-класс в `Infrastructure/Options/`
2. Добавить секцию в `appsettings.json`
3. Зарегистрировать в `Program.cs` или `AddInfrastructure()`:
```csharp
builder.Services
    .AddOptions<MyOptions>()
    .Bind(builder.Configuration.GetSection(MyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## База данных (EF Core)

### AppDbContext

Расположение: `Infrastructure/DB/AppDbContext.cs`

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SampleEntity> Samples => Set<SampleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SampleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
```

### Переключение SQL Server / InMemory

Управляется через конфиг `Database:UseInMemory`:

```csharp
// Infrastructure/Extensions/ServiceCollectionExtensions.cs
if (dbOptions.UseInMemory)
    services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TemplateDb"));
else
    services.AddDbContext<AppDbContext>(o => o.UseSqlServer(dbOptions.ConnectionString));
```

- **Development** — `UseInMemory: true` (не нужен SQL Server)
- **Production** — `UseInMemory: false` (используется SQL Server)

### Миграции

```bash
# Создать миграцию
dotnet ef migrations add Init --project src/Infrastructure --startup-project src/Web

# Применить миграцию
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

### Generic Repository

Интерфейс в `Domain/Interfaces/IRepository.cs`:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

Реализация: `Infrastructure/Repository/GenericRepository.cs`

Регистрация (автоматически для всех сущностей):

```csharp
services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
```

---

## API контроллеры и версионирование

### Версионирование

Все контроллеры поддерживают версионирование через URL:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]                        // fallback без версии
[Authorize]
public class SampleController(IMediator mediator) : ControllerBase
```

Примеры URL:
- `GET /api/v1/sample` — с явной версией
- `GET /api/sample` — дефолтная версия (1.0)

Настройка в `Program.cs`:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;       // Заголовок api-supported-versions в ответе
});
```

### Health Check

`GET /api/health` (или `/api/v1/health`) — без авторизации:

```json
{ "status": "healthy", "timestamp": "2026-06-23T07:00:00Z" }
```

---

## MediatR и CQRS

### Паттерн

Контроллеры не вызывают репозитории напрямую. Вместо этого отправляют **команды/запросы** через `IMediator`:

```
Controller → IMediator.Send(Command/Query) → Handler → IRepository
```

### Структура Features

```
Application/Features/
└── Sample/
    ├── Commands/
    │   ├── CreateSampleCommand.cs  — Command + Validator + Handler
    │   └── DeleteSampleCommand.cs  — Command + Handler
    └── Queries/
        ├── GetAllSamplesQuery.cs   — Query + Handler
        └── GetSampleByIdQuery.cs   — Query + Handler
```

### Пример Command с валидацией

```csharp
// Command (что сделать)
public record CreateSampleCommand(string Name, string? Description) : IRequest<SampleEntity>;

// Validator (FluentValidation — выполняется автоматически через Pipeline)
public class CreateSampleValidator : AbstractValidator<CreateSampleCommand>
{
    public CreateSampleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

// Handler (как сделать)
public class CreateSampleHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<CreateSampleCommand, SampleEntity>
{
    public async Task<SampleEntity> Handle(CreateSampleCommand request, CancellationToken ct)
    {
        var entity = new SampleEntity { Name = request.Name, Description = request.Description };
        await repository.AddAsync(entity, ct);
        return entity;
    }
}
```

### Пример Query

```csharp
public record GetSampleByIdQuery(int Id) : IRequest<SampleEntity?>;

public class GetSampleByIdHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<GetSampleByIdQuery, SampleEntity?>
{
    public async Task<SampleEntity?> Handle(GetSampleByIdQuery request, CancellationToken ct)
        => await repository.GetByIdAsync(request.Id, ct);
}
```

### Использование в контроллере

```csharp
public class SampleController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await mediator.Send(new GetSampleByIdQuery(id), ct);
        return item is null ? NotFound(ApiErrors.NotFound()) : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSampleCommand command, CancellationToken ct)
    {
        var entity = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }
}
```

### Pipeline Behaviors

Выполняются автоматически для каждого MediatR-запроса:

| Behavior | Назначение |
|----------|-----------|
| `ValidationBehavior` | Запускает все `IValidator<TRequest>` перед handler'ом. При ошибке — `ValidationException` |
| `LoggingBehavior` | Логирует `"Handling {RequestName}"` / `"Handled {RequestName}"` |

Порядок: **Validation → Logging → Handler**

---

## Аутентификация (JWT)

Схема: **JWT Bearer** (HMAC-SHA256).

### Настройка

Конфиг в `appsettings.json` → секция `Jwt`:

```json
{
  "Jwt": {
    "Issuer": "TemplateApp",
    "Audience": "TemplateApp.Client",
    "Key": "минимум 32 символа!",
    "AccessTokenMinutes": 60
  }
}
```

### Кастомные ответы

При 401/403 возвращается JSON (не стандартный HTML):

```json
// 401
{ "error": { "code": "UNAUTHORIZED", "message": "Authentication is required" } }

// 403
{ "error": { "code": "FORBIDDEN", "message": "You do not have access to this resource" } }
```

### Как добавить генерацию токенов

В шаблоне нет готового endpoint'а для выдачи JWT (зависит от проекта). Пример реализации:

```csharp
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
    issuer: jwtOptions.Issuer,
    audience: jwtOptions.Audience,
    claims: new[] { new Claim(ClaimTypes.Name, username) },
    expires: DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes),
    signingCredentials: credentials);
return new JwtSecurityTokenHandler().WriteToken(token);
```

---

## Авторизация

### Политики

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Default", policy => policy.RequireAuthenticatedUser());
```

### Использование

```csharp
[Authorize]                     // Требует аутентификацию (дефолтная схема)
[Authorize(Policy = "Default")] // Конкретная политика
[AllowAnonymous]                // Без авторизации (HealthController, Index page)
```

### Antiforgery (для Razor Pages)

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
```

---

## API документация (Swagger + Scalar)

Два UI для просмотра API:

| UI | URL | Описание |
|----|-----|----------|
| Swagger UI | `/swagger` | Классический Swagger, можно тестировать запросы |
| Scalar | `/scalar/v1` | Современный UI для API Reference |

### JWT в Swagger

В Swagger настроена авторизация — кнопка "Authorize" в UI:

```
Вставить токен без префикса "Bearer" → нажать Authorize → запросы будут с заголовком Authorization
```

### Версионирование в Swagger

Каждая API-версия генерирует отдельный документ. Настраивается в `Web/Doc/ConfigureSwaggerOptions.cs`:

```csharp
foreach (var desc in provider.ApiVersionDescriptions)
{
    options.SwaggerDoc(desc.GroupName, new OpenApiInfo
    {
        Title = $"Template API {desc.ApiVersion}",
        Version = desc.ApiVersion.ToString()
    });
}
```

---

## Rate Limiting

Встроенный ASP.NET Core Rate Limiting с `FixedWindowLimiter`:

| Политика | Лимит | Окно | Ключ партиции |
|----------|-------|------|---------------|
| `Api` | 60 запросов | 1 минута | IP-адрес |

При превышении лимита — ответ `429 Too Many Requests`:

```json
{ "error": { "code": "RATE_LIMIT_EXCEEDED", "message": "Too many requests" } }
```

### Применение к контроллеру

```csharp
[EnableRateLimiting("Api")]
public class MyController : ControllerBase { }
```

### Как добавить новую политику

```csharp
options.AddPolicy("Auth", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
```

---

## Логирование (Serilog)

### Конфигурация

Настроена **в коде** (не через appsettings.json), в начале `Program.cs`.

### 3 Sink'а

| Sink | Файл | Что пишет |
|------|------|-----------|
| Console | — | Всё (в терминал) |
| File (app.log) | `Logs/{date}/{time}/app.log` | Всё **кроме** классов с "Api" в имени |
| File (api.log) | `Logs/{date}/{time}/api.log` | **Только** классы с "Api" в имени, с `{StatusCode}` |

### Формат

```
Console:  14:30:15.123 [INF] Приложение запущено
app.log:  14:30:15.123 [INF] (Web.Pages.IndexModel) Загружена страница
api.log:  14:30:15.123 [INF] [200] (Web.Controllers.SampleController) GET /api/sample
```

### Использование в классах

Статический логгер через `Log.ForContext<T>()`:

```csharp
using Serilog;

public class MyService
{
    private static readonly ILogger _log = Log.ForContext<MyService>();

    public void DoWork()
    {
        _log.Information("Обработано записей: {Count}", items.Count);
        _log.Error(ex, "Ошибка обработки");
    }
}
```

### Контекстные свойства (для API)

```csharp
using Serilog.Context;

using (LogContext.PushProperty("StatusCode", 200))
{
    _log.Information("{Method} {Url} -> {StatusCode}", "GET", "/api/sample", 200);
}
```

### Фильтрация

`SourceContextFilter` (`Web/Logging/SourceContextFilter.cs`) разделяет логи по имени класса:

```csharp
SourceContextFilter.Include("Api")  // только классы содержащие "Api"
SourceContextFilter.Exclude("Api")  // все кроме классов содержащих "Api"
```

---

## Razor Pages

### Структура

```
Pages/
├── Index.cshtml / .cs        — Главная (/)
├── Shared/
│   └── _Layout.cshtml        — Общий layout (header, main, scripts)
└── _ViewImports.cshtml       — @addTagHelper, @namespace
```

### Как добавить страницу

1. Создать `Pages/MyPage.cshtml`:
```html
@page
@model Web.Pages.MyPageModel
@{ ViewData["Title"] = "My Page"; Layout = "_Layout"; }
<h1>My Page</h1>
```

2. Создать `Pages/MyPage.cshtml.cs`:
```csharp
public class MyPageModel : PageModel
{
    public void OnGet() { }
}
```

Страница будет доступна по `/MyPage`.

---

## Стандартизированные ошибки API

Все ошибки API возвращаются в едином формате через `Web/Errors/ApiErrors.cs`:

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Sample with id 42 not found"
  }
}
```

### Готовые методы

```csharp
ApiErrors.NotFound("сообщение")         // 404
ApiErrors.BadRequest("сообщение")       // 400
ApiErrors.Unauthorized("сообщение")     // 401
ApiErrors.Forbidden("сообщение")        // 403
ApiErrors.RateLimitExceeded("сообщение") // 429
ApiErrors.Create("CODE", "сообщение")   // Произвольный код
```

### Использование в контроллере

```csharp
return NotFound(ApiErrors.NotFound($"Item with id {id} not found"));
return BadRequest(ApiErrors.BadRequest("Invalid input"));
```

---

## Как добавить новую сущность

Пошаговая инструкция на примере `Product`:

### 1. Domain — сущность и интерфейс (если нужен специфичный)

```csharp
// Domain/Entities/Product.cs
public class Product : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### 2. Infrastructure — DbContext

```csharp
// В AppDbContext.cs добавить:
public DbSet<Product> Products => Set<Product>();

// В OnModelCreating:
modelBuilder.Entity<Product>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
    entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
});
```

### 3. Application — Commands и Queries

```csharp
// Application/Features/Product/Commands/CreateProductCommand.cs
public record CreateProductCommand(string Title, decimal Price) : IRequest<Product>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public class CreateProductHandler(IRepository<Product> repo) : IRequestHandler<CreateProductCommand, Product>
{
    public async Task<Product> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { Title = request.Title, Price = request.Price };
        await repo.AddAsync(product, ct);
        return product;
    }
}
```

```csharp
// Application/Features/Product/Queries/GetAllProductsQuery.cs
public record GetAllProductsQuery : IRequest<IReadOnlyList<Product>>;

public class GetAllProductsHandler(IRepository<Product> repo)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<Product>>
{
    public async Task<IReadOnlyList<Product>> Handle(GetAllProductsQuery request, CancellationToken ct)
        => await repo.GetAllAsync(ct);
}
```

### 4. Web — Controller

```csharp
// Web/Controllers/ProductController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class ProductController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllProductsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd, CancellationToken ct)
    {
        var product = await mediator.Send(cmd, ct);
        return Created($"/api/v1/product/{product.Id}", product);
    }
}
```

### 5. Миграция

```bash
dotnet ef migrations add AddProduct --project src/Infrastructure --startup-project src/Web
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

---

## NuGet пакеты

### Web

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.10 | JWT аутентификация |
| Microsoft.AspNetCore.Mvc.Versioning | 5.1.0 | API Versioning |
| Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer | 5.1.0 | Swagger + Versioning |
| Scalar.AspNetCore | 2.0.18 | Scalar API Reference UI |
| Serilog | 4.3.1 | Структурированное логирование |
| Serilog.Sinks.Console | 6.1.1 | Вывод логов в консоль |
| Serilog.Sinks.File | 7.0.0 | Запись логов в файлы |
| Swashbuckle.AspNetCore | 9.0.6 | Swagger / OpenAPI |

### Application

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| MediatR | 12.4.1 | Mediator pattern (CQRS) |
| FluentValidation | 11.11.0 | Валидация запросов |
| FluentValidation.DependencyInjectionExtensions | 11.11.0 | Авторегистрация валидаторов |

### Infrastructure

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| Microsoft.EntityFrameworkCore | 9.0.11 | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 | SQL Server провайдер |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.11 | InMemory для разработки |
| Microsoft.EntityFrameworkCore.Design | 9.0.11 | Миграции (design-time) |
| Microsoft.Extensions.Hosting | 9.0.10 | IOptions, IConfiguration |

### Domain

Нет NuGet зависимостей (чистый домен).
