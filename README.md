# Events API

REST API для управления мероприятиями. Учебный проект курса «Продвинутая разработка на C# и .NET» (Яндекс Практикум), спринт 1.

## Стек

- .NET 9, ASP.NET Core Web API (контроллеры)
- Swagger (Swashbuckle)
- Хранение данных — в памяти приложения (данные сбрасываются при перезапуске)

## Запуск

Требуется [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/d-trefilov/yaprnet-events-api.git
cd yaprnet-events-api
dotnet build
dotnet run
```

Swagger UI: http://localhost:5199/swagger

## API

| Метод | Адрес | Описание | Ответы |
|---|---|---|---|
| GET | `/events` | Список всех событий | 200 |
| GET | `/events/{id}` | Событие по id | 200, 404 |
| POST | `/events` | Создать событие | 201, 400 |
| PUT | `/events/{id}` | Обновить событие целиком | 200, 400, 404 |
| DELETE | `/events/{id}` | Удалить событие | 204, 404 |

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

## Структура

- `Models/Event.cs` — модель события
- `Services/IEventService.cs`, `Services/EventService.cs` — бизнес-логика и хранение, регистрация в DI как Singleton
- `Controllers/EventsController.cs` — эндпоинты, только вызовы сервиса