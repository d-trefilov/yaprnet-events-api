# Events API

REST API для управления мероприятиями и бронированиями. Учебный проект курса «Продвинутая разработка на C# и .NET» (Яндекс Практикум), спринты 1–5.

## Стек

- .NET 9, ASP.NET Core Web API (контроллеры), Swagger (Swashbuckle)
- PostgreSQL 16, Entity Framework Core 9, Npgsql
- `BackgroundService`, `SemaphoreSlim`, `Task.WhenAll` для обработки броней
- xUnit и EF Core InMemory для юнит-тестов

## Запуск

Нужны [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) и PostgreSQL. Для локальной базы через Docker Compose:

```bash
docker compose up -d --wait
dotnet build EventsApi.slnx
dotnet run --project EventsApi.csproj
```

В `appsettings.json` параметр `ConnectionStrings:DefaultConnection` указывает на локальную учебную базу `eventapi` на порту 5432. Для другого сервера PostgreSQL задайте свою строку подключения через переменную окружения `ConnectionStrings__DefaultConnection`.

При первом запуске EF Core создаёт таблицы `events` и `bookings` через `EnsureCreatedAsync`. Используйте пустую базу для первого запуска. События и брони остаются в PostgreSQL после перезапуска API. Docker Compose хранит данные в именованном томе.

Swagger UI: http://localhost:5199/swagger (при запуске из Visual Studio с профилем `https` — https://localhost:7251/swagger).

## Тесты

```bash
dotnet test EventsApi.slnx
```

Тесты лежат в проекте `EventsApi.Tests` и проверяют сервисы через EF Core InMemory. У каждого теста своя база, а каждый параллельный запрос получает свой контекст. Структура каждого теста — Arrange / Act / Assert.

`EventServiceTests` — события:
- создание (в том числе `availableSeats` равно `totalSeats`), получение всех и по id, обновление, удаление;
- фильтр по названию, по датам, комбинированный фильтр, пагинация;
- ошибки: несуществующий id (404), `EndAt` раньше `StartAt` (400), пустое название (400), `totalSeats` не больше нуля (400), некорректная страница (400).

`BookingServiceTests` — сервис бронирований:
- создание брони для существующего события — статус `Pending`;
- несколько броней на одно событие — у всех разные `Id`;
- получение брони по `Id`;
- получение брони отражает смену статуса после `Confirm` и после `Reject`;
- места: бронь уменьшает `availableSeats` на 1; брони до лимита проходят с уникальными `Id`; после исчерпания мест — `NoAvailableSeatsException`, `availableSeats` остаётся 0;
- `Reject` + `ReleaseSeats` возвращает место, и на него снова можно забронировать;
- ошибки: бронь на несуществующее событие, бронь на удалённое событие, несуществующий `Id` брони — `NotFoundException`.

Тесты на конкурентность (в `BookingServiceTests`) запускают запросы в пуле потоков через `Task.Run` и ждут их вместе через `Task.WhenAll`:
- событие на 5 мест, 20 одновременных запросов — ровно 5 броней, 15 отказов `NoAvailableSeatsException`, `availableSeats` равно 0;
- событие на 10 мест, 10 одновременных запросов — 10 броней с уникальными `Id`.

`BookingTests` — сама сущность `Booking`: `CreatePending`, `Confirm`, `Reject`, запрет повторной обработки.

## API

| Метод | Адрес | Описание | Ответы |
|---|---|---|---|
| GET | `/events` | Список событий с фильтрами и пагинацией | 200, 400 |
| GET | `/events/{id}` | Событие по id | 200, 404 |
| POST | `/events` | Создать событие | 201, 400 |
| PUT | `/events/{id}` | Обновить событие целиком | 200, 400, 404 |
| DELETE | `/events/{id}` | Удалить событие | 204, 404 |
| POST | `/events/{id}/book` | Забронировать место на событие | 202 + `Location`, 404, **409** |
| GET | `/bookings/{id}` | Текущее состояние брони | 200, 404 |

### Фильтрация и пагинация `GET /events`

Все параметры необязательные. Фильтры работают вместе (логическое «И»). Перед пагинацией события сортируются по времени начала, названию и идентификатору. Даты событий хранятся без часового пояса.

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
  "events": [ { "id": "…", "title": "Event 5", "description": null, "startAt": "2026-11-05T10:00:00", "endAt": "2026-11-05T12:00:00", "totalSeats": 10, "availableSeats": 10 } ],
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
| `totalSeats` | int | да, больше нуля |
| `availableSeats` | int | назначается сервером: при создании равно `totalSeats`, уменьшается с каждой бронью |

Значение `availableSeats` из тела запроса игнорируется — его ведёт только сервер.

### Пример запроса на создание

```json
{
  "title": "Митап по .NET",
  "description": "Доклады про ASP.NET Core",
  "startAt": "2026-10-10T18:00:00",
  "endAt": "2026-10-10T21:00:00",
  "totalSeats": 3
}
```

## Бронирования

Бронирование работает по схеме «быстрый ответ + отложенная обработка». Клиент сразу получает ответ `202 Accepted`: заявка принята, место за ней закреплено, но бронь ещё не обработана. Обработку выполняет фоновый сервис, а клиент узнаёт результат по ссылке из заголовка `Location`.

Место занимается в момент создания брони: `availableSeats` события уменьшается на 1. Если свободных мест нет, бронь не создаётся, и API отвечает `409 Conflict`.

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
| `Rejected` | бронь отклонена: событие удалено до обработки или при обработке произошла ошибка |

Статус меняется только методами самой брони: `Confirm()` и `Reject()`. Они заполняют `processedAt` и не дают обработать бронь повторно. В JSON статус передаётся строкой.

### Фоновая обработка

Фоновый сервис опрашивает базу и читает идентификаторы броней со статусом `Pending` в отдельной области зависимостей. Затем запускает задачи через `Task.WhenAll`. Двухсекундная задержка в каждой задаче изображает внешний вызов. После задержки задача открывает свой scope и получает собственный `AppDbContext`.

Если событие доступно, бронь переходит в `Confirmed`. При удалении события его строка остаётся в базе для сохранения связи с бронями. Из API событие исчезает, а ещё не обработанные брони переходят в `Rejected`. При ошибке обработки сервис пытается сохранить отклонение и вернуть место, если событие ещё доступно. Остановка приложения прерывает ожидание. Оставшиеся `Pending` подхватываются при следующем запуске.

## Потокобезопасность

`EventService`, `BookingService` и `AppDbContext` зарегистрированы как scoped. Общий `SemaphoreSlim` синхронизирует создание и обработку броней и удаление события в одном процессе API. При создании брони `SaveChangesAsync` сохраняет новую бронь и изменение остатка мест одной операцией. Задержки фоновых задач проходят вне семафора.

### Сценарий с овербукингом

1. Создать событие на 3 места — `POST /events` с `"totalSeats": 3`, в ответе `"availableSeats": 3`.
2. Три раза вызвать `POST /events/{id}/book` — все три ответа `202 Accepted`.
3. Четвёртый вызов — `409 Conflict`:

```json
{ "status": 409, "detail": "No available seats for this event" }
```

4. `GET /events/{id}` — `"availableSeats": 0`.
5. Через 3–4 секунды `GET /bookings/{id}` для любой из трёх броней — статус `Confirmed`.

То же самое при одновременных запросах проверяет тест на конкурентность: 20 параллельных запросов на 5 мест дают ровно 5 броней.

### Пример сценария одной брони

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
4. Через 3–4 секунды повторить запрос — статус `Confirmed`, `processedAt` заполнен.

## Ошибки

Все ошибки обрабатывает глобальный middleware `GlobalExceptionHandlingMiddleware` и возвращает JSON в формате [Problem Details (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807):

| Ситуация | Исключение | Код |
|---|---|---|
| Неверные данные: даты, пустое название или название длиннее 200 символов, `totalSeats` не больше нуля, `page`, `pageSize` | `ValidationException` | 400 |
| Событие или бронь не найдены | `NotFoundException` | 404 |
| На событии не осталось свободных мест | `NoAvailableSeatsException` | 409 |
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

// POST /events, totalSeats не указан или 0
{ "status": 400, "detail": "Поле TotalSeats должно быть больше нуля." }

// POST /events/{id}/book, мест нет
{ "status": 409, "detail": "No available seats for this event" }

// GET /events?page=0
{ "status": 400, "detail": "Поля page и pageSize не могут быть меньше 1" }
```

Если в теле запроса нет обязательного поля (`title`) или JSON некорректен, запрос отклоняет сам ASP.NET Core ещё до контроллера. Ответ тоже 400 в формате Problem Details, с дополнительным полем `errors`.

## Структура

- `Models/Event.cs` — модель события; места и методы `TryReserveSeats` / `ReleaseSeats`
- `Models/PaginatedResult.cs` — результат с пагинацией
- `Models/Booking.cs`, `Models/BookingStatus.cs` — бронь и её статусы
- `Dto/BookingInfo.cs` — ответ API с информацией о брони
- `DataAccess/AppDbContext.cs`, `DataAccess/Configurations/` — контекст EF Core и маппинг таблиц.
- `Services/IEventService.cs`, `Services/EventService.cs` — бизнес-логика событий: проверка данных, фильтрация, пагинация и операции с PostgreSQL
- `Services/IBookingService.cs`, `Services/BookingService.cs` — создание броней с защитой от овербукинга и получение статуса
- `Services/BookingProcessingService.cs` — параллельная фоновая обработка броней
- `Controllers/EventsController.cs` — эндпоинты событий и бронирование `POST /events/{id}/book`, только вызовы сервисов
- `Controllers/BookingsController.cs` — `GET /bookings/{id}`
- `Exceptions/` — `NotFoundException`, `ValidationException`, `NoAvailableSeatsException`
- `Middleware/GlobalExceptionHandlingMiddleware.cs` — глобальная обработка ошибок