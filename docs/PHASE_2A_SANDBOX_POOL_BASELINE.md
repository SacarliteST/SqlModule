# SBOX-2A-001: baseline one-shot sandbox

Дата локального замера: 2026-09-12. Ветка: `Phase-2`, исходный production-код:
`a8f1025`. Замер выполнен только на локальном Docker Desktop; VPS не использовался.

## Как воспроизвести

Из корня `Backend/SqlModule` выполнить одной командой PowerShell:

```powershell
$env:SQLMODULE_SANDBOX_BASELINE='true'; $env:SQLMODULE_SANDBOX_BASELINE_ITERATIONS='5'; dotnet test IntegrationTests/IntegrationTests.csproj --filter "Category=SandboxBaseline" --logger "console;verbosity=detailed"
```

Без `SQLMODULE_SANDBOX_BASELINE=true` тест завершается без запуска контейнеров, поэтому обычный
`dotnet test` не получает скрытую Docker-нагрузку. Число итераций можно менять через
`SQLMODULE_SANDBOX_BASELINE_ITERATIONS`; по умолчанию выполняется 5 итераций каждого сценария.
Для итогового сравнения в `SBOX-2A-012` следует задать не менее 100 итераций.

Тест и входные данные версионируются в
`IntegrationTests/Sandbox/SandboxOneShotBaselineTests.cs`. Один и тот же dataset применяется к
PostgreSQL и MySQL:

- `departments`: 2 строки;
- `employees`: 3 строки и внешний ключ на `departments`;
- запрос: `LEFT JOIN`, группировка и подсчёт сотрудников по отделу;
- introspection: две таблицы и одна связь;
- лимит результата: 100 строк, таймаут SQL: 30 секунд.

Каждая измеряемая операция вызывает настоящий `TestcontainersSandboxExecutor`. Время включает
полный вызов: создание и старт контейнера, readiness, подключение, setup/операцию и удаление
контейнера. Перцентили считаются методом nearest-rank. Throughput измеряется для
последовательного сценария целиком. Параллельно локальный `docker stats` снимает максимальные CPU
и RAM контейнера; сбор метрик является одинаковой частью методики для будущего pooled-прогона.

## Стенд

| Параметр | Значение |
|---|---|
| ОС | Windows 10, .NET SDK 10.0.109 |
| CPU | 12 логических процессоров |
| Docker | Docker Desktop Engine 28.3.0, API 1.51 |
| Docker kernel | WSL2 6.6.87.2 |
| Память Docker-host | 15.46 GiB |
| PostgreSQL | `postgres:15-alpine` |
| MySQL | `mysql:8.0` |
| Конкурентность | 1, последовательный прогон |

## Результаты

| DBMS | Операция | p50, ms | p95, ms | p99, ms | ops/s | Старты | Peak CPU | Peak RAM |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| PostgreSQL | `RunAsync` | 3 270 | 4 488 | 4 488 | 0.282 | 5 | n/a | n/a |
| PostgreSQL | `ValidateSetupAsync` | 3 721 | 7 626 | 7 626 | 0.222 | 5 | n/a | n/a |
| PostgreSQL | `InspectDdlAsync` | 3 649 | 5 403 | 5 403 | 0.257 | 5 | n/a | n/a |
| MySQL | `RunAsync` | 16 311 | 20 282 | 20 282 | 0.060 | 5 | 132.16% | 394.3 MiB |
| MySQL | `ValidateSetupAsync` | 13 324 | 14 451 | 14 451 | 0.075 | 5 | 126.48% | 388.8 MiB |
| MySQL | `InspectDdlAsync` | 13 297 | 14 573 | 14 573 | 0.073 | 5 | 128.00% | 389.3 MiB |

Docker показывает CPU относительно одного ядра, поэтому значение многопоточного старта может
превышать 100%.

Полный прогон занял 4 минуты 39 секунд. Процесс test runner использовал 10.141 секунды CPU и
116.8 MiB working set на момент завершения. Для шести сценариев по пять итераций создано ровно
30 контейнеров СУБД, не считая служебного Ryuk. После теста контейнеры операций удалены.

У PostgreSQL one-shot контейнер удаляется раньше, чем `docker stats --no-stream` успевает вернуть
сэмпл. Поэтому его ресурсный отпечаток дополнительно проверен на том же образе после readiness:
28.63 MiB RAM и 0.04–0.08% CPU в состоянии покоя. Это не peak старта и не подменяет будущую
полноценную телеметрию `SBOX-2A-009`; ограничение явно сохраняется для честного сравнения.

## Замороженная семантика совместимости

Публичный интерфейс `ISandboxExecutor` и его методы `RunAsync`, `ValidateSetupAsync`,
`InspectDdlAsync` в Фазе 2a не меняются.

### Общие ошибки

| Ситуация | Внешний `Result` | Код | Тип ошибки |
|---|---|---|---|
| Неизвестный `SystemName` | `IsSuccess=false` | `Sandbox.UnsupportedDbms` | `Failure` |
| Контейнер не стартовал или подключение не открылось | `IsSuccess=false` | `Sandbox.ContainerFailed` | `Failure` |
| DDL/seed не применился | `IsSuccess=false` | `Sandbox.SetupFailed` | `Validation` |

Текст низкоуровневой ошибки сейчас находится в `Error.Message`. Фаза 2a не меняет существующий
HTTP-маппинг и безопасную публикацию ошибок вышележащим API.

### `RunAsync`

- Успех инфраструктуры и SQL: `Result<QueryResultSet>.IsSuccess=true`,
  `QueryResultSet.Succeeded=true`.
- Ошибка пользовательского SQL не является инфраструктурным `Result.Fail`: внешний результат
  остаётся успешным, а `QueryResultSet.Succeeded=false` и `Error` содержит ошибку SQL.
- Колонки сохраняют порядок reader, ячейки представлены строками и допускают `null`.
- `MaxRows` ограничивает возвращённые строки; наличие следующей строки выставляет
  `IsTruncated=true`; `RowCount` равен числу фактически возвращённых строк.
- `DurationMs` измеряет SQL-часть, а не полный жизненный цикл контейнера.
- Запрос выполняется в read-only транзакции; в `finally` обязательно делается попытка `ROLLBACK`.

### `ValidateSetupAsync`

- Корректный setup возвращает `Result.Success()` без значения.
- Ошибка любого setup-оператора возвращает `Sandbox.SetupFailed`.
- Операторы применяются последовательно и прекращаются на первой ошибке.

### `InspectDdlAsync`

- DDL сначала применяется тем же setup-механизмом, затем читается системный каталог.
- Успех возвращает `InspectedSchema` с таблицами, колонками и внешними ключами в текущем порядке.
- Ошибка DDL или чтения каталога возвращает `Sandbox.SetupFailed`.

Пул обязан сохранить эту матрицу результатов и ошибок. Новая внутренняя диагностика не должна
расширять публичные HTTP-контракты, Swagger или Orval в Фазе 2a.
