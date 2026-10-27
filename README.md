# Events API

REST API для управления мероприятиями. Учебный проект курса «Продвинутая разработка на C# и .NET» (Яндекс Практикум), спринты 1–2.

## Стек

- .NET 9, ASP.NET Core Web API (контроллеры)
- Swagger (Swashbuckle)
- xUnit — юнит-тесты
- Хранение данных — в памяти приложения (данные сбрасываются при перезапуске)

## Запуск

Требуется [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/d-trefilov/yaprnet-events-api.git
cd yaprnet-events-api
dotnet build
dotnet run
```

Swagger UI: http://localhost:5199/swagger (при запуске из Visual Studio с профилем `https` — https://localhost:7251/swagger).

## Тесты

```bash
dotnet test
```

Тесты лежат в проекте `EventsApi.Tests` и проверяют `EventService` напрямую, без HTTP:
- создание, получение всех и по id, обновление, удаление;
- фильтр по названию, по датам, комбинированный фильтр, пагинация;
- ошибки: несуществующий id (404), `EndAt` раньше `StartAt` (400), некорректная страница (400).

## API

| Метод | Адрес | Описание | Ответы |
|---|---|---|---|
| GET | `/events` | Список событий с фильтрами и пагинацией | 200, 400 |
| GET | `/events/{id}` | Событие по id | 200, 404 |
| POST | `/events` | Создать событие | 201, 400 |
| PUT | `/events/{id}` | Обновить событие целиком | 200, 400, 404 |
| DELETE | `/events/{id}` | Удалить событие | 204, 404 |

### Фильтрация и пагинация `GET /events`

Все параметры необязательные. Фильтры работают вместе (логическое «И»).

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `title` | string | — | часть названия, без учёта регистра |
| `from` | DateTime | — | события, которые начинаются не раньше этой даты |
| `to` | DateTime | — | события, которые заканчиваются не позже этой даты |
| `page` | int | 1 | номер страницы, от 1 |
| `pageSize` | int | 10 | событий на странице, от 1 |

Примеры:

```
GET /events?title=c%23
GET /events?from=2026-11-05&to=2026-11-30
GET /events?title=c%23&from=2026-11-05
GET /events?page=2&pageSize=2
```

Пример ответа на `GET /events?page=3&pageSize=2` (всего 5 событий):

```json
{
  "totalCount": 5,
  "events": [ { "id": "…", "title": "Event 5", "description": null, "startAt": "2026-11-05T10:00:00", "endAt": "2026-11-05T12:00:00" } ],
  "pageNumber": 3,
  "itemsOnPage": 1
}
```

- `totalCount` — сколько событий нашёл фильтр всего;
- `itemsOnPage` — сколько событий на этой странице.

### Модель события

| Поле | Тип | Обязательное |
|---|---|---|
| `id` | Guid | назначается сервером |
| `title` | string | да |
| `description` | string | нет |
| `startAt` | DateTime | да |
| `endAt` | DateTime | да, позже `startAt` |

### Пример запроса на создание

```json
{
  "title": "Митап по .NET",
  "description": "Доклады про ASP.NET Core",
  "startAt": "2026-10-10T18:00:00",
  "endAt": "2026-10-10T21:00:00"
}
```

## Ошибки

Все ошибки обрабатывает глобальный middleware `GlobalExceptionHandlingMiddleware` и возвращает JSON в формате [Problem Details (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807):

| Ситуация | Исключение | Код |
|---|---|---|
| Неверные данные: даты, `page`, `pageSize` | `ValidationException` | 400 |
| Событие не найдено | `NotFoundException` | 404 |
| Любая непредвиденная ошибка | любое другое | 500 |

Ошибка логируется через `ILogger` с методом и путём запроса.

Примеры:

```json
// GET /events/00000000-0000-0000-0000-000000000001
{ "status": 404, "detail": "Событие 00000000-0000-0000-0000-000000000001 не найдено" }

// POST /events, endAt раньше startAt
{ "status": 400, "detail": "EndAt должен быть позже StartAt." }

// GET /events?page=0
{ "status": 400, "detail": "Поля page и pageSize не могут быть меньше 1" }
```

Если в теле запроса нет обязательного поля (`title`) или JSON некорректен, запрос отклоняет сам ASP.NET Core ещё до контроллера. Ответ тоже 400 в формате Problem Details, с дополнительным полем `errors`.

## Структура

- `Models/Event.cs` — модель события
- `Models/PaginatedResult.cs` — результат с пагинацией
- `Services/IEventService.cs`, `Services/EventService.cs` — бизнес-логика: проверка данных, фильтрация, пагинация, хранение; регистрация в DI как Singleton
- `Controllers/EventsController.cs` — эндпоинты, только вызовы сервиса
- `Exceptions/` — `NotFoundException`, `ValidationException`
- `Middleware/GlobalExceptionHandlingMiddleware.cs` — глобальная обработка ошибок
- `EventsApi.Tests/` — юнит-тесты xUnit