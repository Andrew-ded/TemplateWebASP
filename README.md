# TemplateWebASP
Шаблон для проектов ASP.NET с подготовленными предварительными настройками

## Структура решения (Clean Architecture)
```
src/
├── Web/            — Точка входа, контроллеры API, Razor Pages, Swagger, авторизация
├── Application/    — Сервисный слой (бизнес-логика)
├── Domain/         — Доменные модели, интерфейсы
└── Infrastructure/ — Репозитории, DbContext, Options, конфигурация БД
```

Зависимости: `Web → Application → Infrastructure → Domain`

## Основные элементы
- **Swagger + Scalar** — OpenAPI документация с двумя UI (/swagger и /scalar/v1)
- **Clean Architecture** — 4 проекта: Web, Application, Domain, Infrastructure
- **EF Core** — SQL Server + InMemory для разработки
- **DbContext** — базовый контекст с Fluent API конфигурацией
- **ControllerBase** — версионированные API контроллеры
- **Razor Pages** — с Layout, ViewImports, Antiforgery
- **IOptions<T>** — с `ValidateDataAnnotations()` + `ValidateOnStart()`

## Дополнительно
- **MediatR + CQRS** — Commands/Queries через Mediator, Pipeline Behaviors (валидация, логирование)
- **FluentValidation** — валидация запросов через Pipeline Behavior
- **Serilog** — Console + File sinks, разделение логов: `app.log` (приложение) и `api.log` (API с StatusCode), папка `Logs/{date}/{time}/`
- **JWT аутентификация** — настроенный JwtBearer с кастомными ответами на 401/403
- **API Versioning** — версионирование через URL (`/api/v1/...`)
- **Rate Limiting** — FixedWindow политики для защиты API
- **Стандартизированные ошибки** — единый формат `ApiErrorResponse` для всех ошибок API
- **Authorization Policies** — подготовленный builder политик авторизации
- **ProblemDetails** — стандартная обработка ошибок ASP.NET Core
- **Generic Repository** — базовая реализация `IRepository<T>`
- **DI Extensions** — `AddInfrastructure()` / `AddApplication()` для чистой регистрации сервисов
- **Offline/InMemory режим** — переключение через конфиг `Database:UseInMemory`

## Быстрый старт
```bash
cd src/Web
dotnet run
```
- Swagger UI: http://localhost:5000/swagger
- Scalar: http://localhost:5000/scalar/v1
- Главная: http://localhost:5000/
