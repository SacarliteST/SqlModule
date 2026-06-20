# SQLModule — концепция и документация

## 1. Контекст: платформа Scoodle

**Scoodle** — интеллектуальная система для обучения и оценки знаний, разрабатываемая в рамках магистерской диссертации. Платформа строится на принципе «Хост-Плагин»: ядро выступает диспетчером, а специализированные учебные модули подключаются как независимые единицы через открытый API.

Ключевая идея Scoodle — преодоление разрыва между классическим тестированием и отработкой практических навыков. Вместо простой проверки теоретических знаний платформа фиксирует **цифровой след** обучающегося (последовательность действий, ошибки, время реакции) и на его основе формирует интеллектуальную оценку.

### Роли пользователей

| Роль | Функции |
|---|---|
| Обучаемый | Изучение теории, выполнение практических заданий, прохождение тестирования |
| Преподаватель | Формирование контента, мониторинг успеваемости через протоколы цифрового следа |
| Администратор | Управление учётными записями, конфигурирование внешних модулей через API |

### Архитектура Scoodle (выбранный подход)

Выбрана **микромодульная событийная архитектура (EDA — Event-Driven Architecture)**, позволяющая:
- подключать и отключать практические модули без перезапуска ядра;
- мгновенно реагировать на действия пользователя через WebSockets (SignalR);
- хранить цифровой след в PostgreSQL (тип данных `JSONB`).

Базовые CRUD-операции работают через REST/HTTP. Передача событий в реальном времени — через SignalR.

---

## 2. SQLModule — практический модуль SQL-тренажёра

**SQLModule** — это пилотный практический модуль платформы Scoodle, предоставляющий обучающемуся интерактивную среду («песочницу») для работы с реальными реляционными базами данных.

### Что умеет модуль

- Управление конфигурациями СУБД (справочник СУБД — Docker-образ, порты, переменные окружения).
- Управление целевыми базами данных (TargetDb) — подключение к реальным экземплярам БД.
- Описание схемы данных (мета-таблицы, мета-атрибуты).
- Маппинг физических типов данных (PhysicalType) с параметрами конфигурации (ParameterDefinition).
- **Проверка Docker-конфигурации СУБД** через Testcontainers (поднимает реальный контейнер и выполняет `SELECT 1`).

### Место в архитектуре Scoodle

```
Ядро Scoodle (C#, ASP.NET Core)
│
├── Инициализация сессии  →  SQL Модуль (SQLModule API)
│       REST: { module_id, task_id, session_id, access_token }
│
├── Цифровой след         ←  SQL Модуль
│       WebSocket/REST: { session_id, event_type, payload }
│       Пример payload: { input_code: "SELECT ...", rows_affected: 25 }
│
└── Завершение задания    ←  SQL Модуль
        REST: { status: "COMPLETED", final_score: 95.0 }
```

---

## 3. Доменная модель SQLModule

### DbmsCatalog — каталог СУБД

#### `DbmsDictionary` — справочник систем управления базами данных

Центральный агрегат модуля. Описывает СУБД, которую можно поднять в Docker-контейнере.

| Поле | Описание |
|---|---|
| `DbmsName` | Человекочитаемое название (напр., «PostgreSQL 16») |
| `DbmsSystemName` | Системный идентификатор (напр., `postgres`) |
| `DockerImage` | Docker-образ (напр., `postgres:16`) |
| `DefaultPort` | Порт, открываемый в контейнере (напр., `5432`) |
| `EnvUserKey` / `EnvPasswordKey` / `EnvDatabaseKey` | Имена переменных окружения для настройки контейнера |
| `ExtraEnvConfig` | Дополнительные переменные окружения в формате `KEY=VALUE;KEY2=VALUE2` |
| `DefaultDatabase` | База данных по умолчанию |
| `DefaultUsername` | Пользователь по умолчанию |
| `DefaultPassword` | Пароль (не возвращается в ответах API — соображения безопасности) |

#### `PhysicalType` — физический тип данных

Тип данных конкретной СУБД (напр., `VARCHAR`, `INT`, `JSONB`). Привязан к `DbmsDictionary`.

#### `ParameterDefinition` — параметр конфигурации физического типа

Описывает параметры, которые можно настроить для физического типа (напр., максимальная длина для `VARCHAR`). Содержит:
- ключ параметра и отображаемое имя;
- тип ввода (`text`, `number`, и т.д.);
- SQL-фрагмент для подстановки;
- коэффициент сортировки.

### Schema — описание схемы данных

#### `TargetDb` — целевая база данных

Конкретная БД, к которой подключается SQL-тренажёр. Хранит параметры подключения и привязана к `DbmsDictionary`.

#### `MetaTable` — мета-таблица

Описание логической таблицы в `TargetDb`. Не является реальной таблицей в БД — это мета-описание схемы.

#### `MetaAttribute` — мета-атрибут

Описание колонки в `MetaTable`. Содержит:
- `PhysicalType` — физический тип данных;
- флаги `IsNullable`, `IsPrimaryKey`;
- значения параметров (`CellValue`) для кастомизации типа (напр., длина VARCHAR).

---

## 4. Технологический стек

| Слой | Технология |
|---|---|
| Язык и платформа | C# 13 / .NET 10 |
| Веб-фреймворк | ASP.NET Core 10 — Minimal API |
| ORM | Entity Framework Core 10 + Npgsql |
| СУБД | PostgreSQL |
| Контейнеры (песочница) | Testcontainers 4.6.0 |
| Соединение с БД (проб) | Npgsql 10.0.3 |
| IDE | JetBrains Rider |
| Тестирование | xUnit + Shouldly + Testcontainers |

### Почему PostgreSQL

PostgreSQL выбран как гибридное решение:
- реляционные таблицы обеспечивают целостность структуры справочников;
- тип `JSONB` позволяет хранить неструктурированный цифровой след (`PracticalTaskEvents.data`);
- бесплатен, кроссплатформен, поддерживает ACID и богатый набор индексов.

---

## 5. Механизм проверки конфигурации СУБД (Sandbox / IDbmsProbe)

Перед сохранением новой конфигурации СУБД (`DbmsDictionary`) система проверяет, что Docker-образ действительно работает.

### Как это устроено

```
CreateDbmsDictionary (HTTP POST)
│
├── Валидация запроса (FluentValidation)
├── Проверка дубликата имени (AlreadyExists?)
├── IDbmsProbe.ProbeAsync(spec)          ← ключевой шаг
│       ├── Если ProbeEnabled=false → Result.Success() сразу
│       ├── Если кеш попал → Result.Success() сразу
│       └── Иначе:
│               1. ContainerBuilder → запускает Docker-контейнер
│               2. Ждёт открытия порта (WaitStrategy)
│               3. NpgsqlConnection → SELECT 1
│               4. Кеширует результат (IMemoryCache, TTL = ProbeCacheTtl)
│               5. Останавливает контейнер
├── На ошибке → 409 Conflict (DbmsDictionary.ProbeFailed)
└── На успехе → сохраняет в БД, возвращает DbmsDictionaryResponse
```

### Конфигурация (appsettings.json, секция `DbmsCatalog`)

```json
{
  "DbmsCatalog": {
    "ProbeEnabled": true,
    "ProbeTimeoutSeconds": 120,
    "ProbeCacheTtl": "01:00:00"
  }
}
```

### Зачем нужен `AlwaysOkProbe` в тестах

`TestcontainersDbmsProbe` поднимает реальный Docker-контейнер (30–120 секунд). Множество тестов используют `CreateDbmsDictionaryAsync()` как вспомогательный метод для создания зависимых сущностей. Запуск контейнера при каждом таком вызове сделал бы suite непрактично медленным.

Решение: тестовый хост переопределяет `IDbmsProbe` на `AlwaysOkProbe` (всегда возвращает `Result.Success()`). Реальные Docker-тесты вынесены в отдельный класс `DbmsProbeTests`, который не входит в `[Collection]` и запускается независимо.

---

## 6. Архитектурные паттерны

### CQRS (Command/Query Responsibility Segregation)

Каждая операция реализована как отдельный `IRequest<T>` / `IRequestHandler<TReq, TRes>`, передаваемый через `ISender` (MediatR).

```
Endpoint → ISender.Send(new CreateDbmsDictionaryCommand(req))
                └→ CreateDbmsDictionaryHandler
                        ├── Проверка дубликата
                        ├── IDbmsProbe.ProbeAsync
                        └── DbContext.SaveChanges
```

### Result pattern

Все операции возвращают `Result` или `Result<T>` вместо исключений для ожидаемых ошибок.

| HTTP-код | Тип ошибки |
|---|---|
| 422 Unprocessable Entity | `ValidationException` |
| 404 Not Found | `NotFound` |
| 409 Conflict | `Conflict` (AlreadyExists, InUse, ProbeFailed) |
| 500 Internal Server Error | `Failure` |

### Типизированный HTTP-клиент

`IDbmsDictionaryClient` наследует `CrudClientBase<Create, Update, Response>` и добавляет метод `ValidateAsync`. `ErrorDelegatingHandler` перехватывает не-2xx ответы и преобразует их в типизированные исключения (`ValidationException`, `ConflictException`, `NotFoundException`, `ApiException`).

### DomainErrors

Коды ошибок генерируются автоматически через `[CallerMemberName]`:

```csharp
// Код ошибки: "DbmsDictionary.NotFound"
internal static Error NotFound(Guid id) => DomainErrors<DbmsDictionary>.NotFound(id);
```

---

## 7. API — маршруты

Базовый путь: `/api/dbms-catalog/dbms-dictionaries`

| Метод | Маршрут | Описание |
|---|---|---|
| `POST` | `/` | Создать СУБД (с проверкой Docker) |
| `POST` | `/validate` | Проверить Docker-конфигурацию без сохранения |
| `GET` | `/{id}` | Получить СУБД по ID |
| `GET` | `/` | Постраничный список СУБД |
| `PUT` | `/{id}` | Обновить СУБД (с проверкой Docker) |
| `DELETE` | `/{id}` | Удалить СУБД (если не используется) |

Удаление блокируется, если `DbmsDictionary` используется хотя бы одной `TargetDb` или одним `PhysicalType`.

---

## 8. Структура проекта

```
SQLModule/
├── Contracts/           — DTO (Request/Response), маршруты API
│   └── DbmsCatalog/
│       └── DbmsDictionary/
├── Domain/              — Доменные сущности (агрегаты, фабричные методы Create)
│   └── DbmsCatalog/
├── Data/                — EF Core: AppDbContext, конфигурации таблиц, миграции
├── Host/                — ASP.NET Core приложение
│   ├── Features/        — Вертикальные срезы (Feature Folders)
│   │   └── DbmsCatalog/
│   │       ├── DbmsDictionary/
│   │       │   ├── Sandbox/    — IDbmsProbe, TestcontainersDbmsProbe, DbmsCatalogOptions
│   │       │   ├── Slices/     — Create, Update, Delete, GetById, GetAll, Validate
│   │       │   ├── DbmsDictionaryErrors.cs
│   │       │   ├── DbmsDictionaryMappings.cs
│   │       │   └── DbmsDictionariesModule.cs
│   │       └── PhysicalTypes/
│   └── Common/          — ResultExtensions, ValidationFilter, FeaturesExtensions
├── Client/              — Типизированные HTTP-клиенты для тестов и фронтенда
│   └── DbmsDictionary/
│       ├── IDbmsDictionaryClient.cs
│       └── DbmsDictionaryClient.cs
├── IntegrationTests/    — Интеграционные тесты
│   ├── infrastructure/  — TestApplication, ApiTestBase, AlwaysOkProbe
│   └── DbmsCatalog/
│       └── DbmsDictionary/
│           ├── DbmsDictionaryTests.cs  — 13 тестов CRUD + Validate
│           └── DbmsProbeTests.cs       — 4 теста с реальным Docker
└── UnitTests/           — Модульные тесты
```

---

## 9. Интеграционные тесты

### Подход

Все тесты наследуют `ApiTestBase` и работают через типизированных клиентов (`IDbmsDictionaryClient`, `IPhysicalTypeClient`, и т.д.). `TestApplication` запускает реальный ASP.NET Core хост с тестовой PostgreSQL базой данных и заменяет `IDbmsProbe` на `AlwaysOkProbe`.

### DbmsProbeTests — тесты с реальным Docker

| Тест | Описание |
|---|---|
| `ProbeAsync_ValidPostgresConfig_ReturnsSuccess` | Поднимает `postgres:latest`, выполняет `SELECT 1` (~30–60 с) |
| `ProbeAsync_InvalidImage_ReturnsProbeFailed` | Несуществующий образ → `Result.Fail` |
| `ProbeAsync_ProbeDisabled_ReturnsSuccessWithoutContainer` | `ProbeEnabled=false` → мгновенный успех без Docker |
| `ProbeAsync_SecondCall_ReturnsCachedSuccess` | Повторный вызов возвращает результат из `IMemoryCache` |

---

## 10. Контекст разработки

- **Студент:** Боковой Владислав Сергеевич, группа 459м
- **Учебное заведение:** Санкт-Петербургский государственный технологический институт
- **Направление:** 09.04.01 Информатика и вычислительная техника, «Информационное и программное обеспечение автоматизированных систем»
- **SQLModule** является пилотным практическим модулем платформы **Scoodle** и одновременно элементом магистерской диссертации — демонстрирует концепцию «песочницы» для отработки SQL-навыков с фиксацией цифрового следа.
