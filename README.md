# Events API

REST API для управления мероприятиями и бронированиями. Учебный проект курса «Продвинутая разработка на C# и .NET» (Яндекс Практикум), спринты 1–3.

## Стек

- .NET 9, ASP.NET Core Web API (контроллеры)
- Swagger (Swashbuckle)
- xUnit — юнит-тесты
- `BackgroundService` — фоновая обработка бронирований
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

Тесты лежат в проекте `EventsApi.Tests` и проверяют логику напрямую, без HTTP. Структура каждого теста — Arrange / Act / Assert.

`EventServiceTests` — события:
- создание, получение всех и по id, обновление, удаление;
- фильтр по названию, по датам, комбинированный фильтр, пагинация;
- ошибки: несуществующий id (404), `EndAt` раньше `StartAt` (400), пустое название (400), некорректная страница (400).

`BookingServiceTests` — сервис бронирований:
- создание брони для существующего события — статус `Pending`;
- несколько броней на одно событие — у всех разные `Id`;
- получение брони по `Id`;
- получение брони отражает смену статуса после `Confirm` и после `Reject`;
- ошибки: бронь на несуществующее событие, бронь на удалённое событие, несуществующий `Id` брони — `NotFoundException`.

`BookingTests` — сама сущность `Booking`: `CreatePending`, `Confirm`, `Reject`, запрет повторной обработки.

## API

| Метод | Адрес | Описание | Ответы |
|---|---|---|---|
| GET | `/events` | Список событий с фильтрами и пагинацией | 200, 400 |
| GET | `/events/{id}` | Событие по id | 200, 404 |
| POST | `/events` | Создать событие | 201, 400 |
| PUT | `/events/{id}` | Обновить событие целиком | 200, 400, 404 |
| DELETE | `/events/{id}` | Удалить событие | 204, 404 |
| POST | `/events/{id}/book` | Забронировать место на событие | 202 + `Location`, 404 |
| GET | `/bookings/{id}` | Текущее состояние брони | 200, 404 |

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
| `title` | string | да, не пустое |
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

## Бронирования

Бронирование работает по схеме «быстрый ответ + отложенная обработка». Клиент сразу получает ответ `202 Accepted`: заявка принята, но ещё не обработана. Обработку выполняет фоновый сервис, а клиент узнаёт результат по ссылке из заголовка `Location`.

### Модель брони

| Поле | Тип | Описание |
|---|---|---|
| `id` | Guid | идентификатор брони |
| `eventId` | Guid | событие, к которому относится бронь |
| `status` | `BookingStatus` | текущий статус |
| `createdAt` | DateTime (UTC) | когда бронь создана |
| `processedAt` | DateTime? (UTC) | когда бронь обработана; `null`, пока не обработана |

### Статусы `BookingStatus`

| Статус | Значение |
|---|---|
| `Pending` | бронь создана, ожидает обработки |
| `Confirmed` | бронь подтверждена |
| `Rejected` | бронь отклонена |

Статус меняется только методами самой брони: `Confirm()` и `Reject()`. Они заполняют `processedAt` и не дают обработать бронь повторно. В JSON статус передаётся строкой.

### Фоновая обработка

`BookingProcessingService` (наследник `BackgroundService`, регистрируется через `AddHostedService`):

1. Раз в секунду запрашивает в хранилище брони в статусе `Pending`.
2. Для каждой выполняет задержку 2 секунды — имитацию обращения к внешней системе.
3. Переводит бронь в `Confirmed`, заполняет `processedAt` и сохраняет её в хранилище.

Ошибка при обработке одной брони записывается в лог и не останавливает обработку остальных. При остановке приложения ожидание прерывается сразу через `CancellationToken`. Каждое подтверждение пишется в лог через `ILogger`.

Хранилище броней (`InMemoryBookingRepository`) построено на `ConcurrentDictionary`, потому что с ним одновременно работают запросы API и фоновый сервис.

### Пример сценария

1. Создать событие — `POST /events`, в ответе `201` и `id` события.
2. Забронировать — `POST /events/{id}/book`:

```
HTTP/1.1 202 Accepted
Location: /bookings/3af70a57-a53d-417e-91af-38347f83861a
```

```json
{
  "id": "3af70a57-a53d-417e-91af-38347f83861a",
  "eventId": "7903de12-9e9d-4a5a-8c19-700e6d472b7b",
  "status": "Pending",
  "createdAt": "2026-10-30T11:56:15.2243661Z",
  "processedAt": null
}
```

3. Сразу запросить `GET /bookings/{id}` — статус `Pending`.
4. Через 3–4 секунды повторить запрос — статус `Confirmed`, `processedAt` заполнен:

```json
{
  "id": "3af70a57-a53d-417e-91af-38347f83861a",
  "eventId": "7903de12-9e9d-4a5a-8c19-700e6d472b7b",
  "status": "Confirmed",
  "createdAt": "2026-10-30T11:56:15.2243661Z",
  "processedAt": "2026-10-30T11:56:17.9001234Z"
}
```

Бронь на несуществующее событие и запрос несуществующей брони возвращают `404`.

## Ошибки

Все ошибки обрабатывает глобальный middleware `GlobalExceptionHandlingMiddleware` и возвращает JSON в формате [Problem Details (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807):

| Ситуация | Исключение | Код |
|---|---|---|
| Неверные данные: даты, пустое название, `page`, `pageSize` | `ValidationException` | 400 |
| Событие или бронь не найдены | `NotFoundException` | 404 |
| Любая непредвиденная ошибка | любое другое | 500 |

Ошибка логируется через `ILogger` с методом и путём запроса.

На `500` клиент получает общий текст «Внутренняя ошибка сервера». Подробности исключения пишутся только в лог, чтобы не раскрывать внутреннее устройство сервиса.

Примеры:

```json
// GET /events/00000000-0000-0000-0000-000000000001
{ "status": 404, "detail": "Событие 00000000-0000-0000-0000-000000000001 не найдено" }

// GET /bookings/00000000-0000-0000-0000-000000000001
{ "status": 404, "detail": "Бронь 00000000-0000-0000-0000-000000000001 не найдена" }

// POST /events, endAt раньше startAt
{ "status": 400, "detail": "EndAt должен быть позже StartAt." }

// GET /events?page=0
{ "status": 400, "detail": "Поля page и pageSize не могут быть меньше 1" }
```

Если в теле запроса нет обязательного поля (`title`) или JSON некорректен, запрос отклоняет сам ASP.NET Core ещё до контроллера. Ответ тоже 400 в формате Problem Details, с дополнительным полем `errors`.

## Структура

- `Models/Event.cs` — модель события
- `Models/PaginatedResult.cs` — результат с пагинацией
- `Models/Booking.cs`, `Models/BookingStatus.cs` — бронь и её статусы
- `Dto/BookingInfo.cs` — ответ API с информацией о брони
- `DataAccess/IBookingRepository.cs`, `DataAccess/InMemoryBookingRepository.cs` — потокобезопасное хранилище броней в памяти; Singleton
- `Services/IEventService.cs`, `Services/EventService.cs` — бизнес-логика событий: проверка данных, фильтрация, пагинация, хранение; Singleton
- `Services/IBookingService.cs`, `Services/BookingService.cs` — создание и получение броней; Singleton
- `Services/BookingProcessingService.cs` — фоновая обработка броней
- `Controllers/EventsController.cs` — эндпоинты событий и бронирование `POST /events/{id}/book`, только вызовы сервисов
- `Controllers/BookingsController.cs` — `GET /bookings/{id}`
- `Exceptions/` — `NotFoundException`, `ValidationException`
- `Middleware/GlobalExceptionHandlingMiddleware.cs` — глобальная обработка ошибок
