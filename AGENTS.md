# AGENTS.md

Инструкции для агентской разработки backend SQLModule.

Перед крупной или незнакомой задачей прочитать `CONCEPT.md`. Для границ текущих фаз использовать `ROADMAP.md`; для межсервисной интеграции — корневой `../../PLATFORM.md`. Старые документы и техдолг считать историческими, если задача прямо на них не ссылается.

## Репозиторные skills

Перед работой загружать минимальный подходящий набор инструкций:

- [`focused-feature-flow`](.agents/skills/focused-feature-flow/SKILL.md) — экономный цикл реализации, таргетные проверки и один полный финальный прогон;
- [`sqlmodule-development`](.agents/skills/sqlmodule-development/SKILL.md) — слои SQLModule, sandbox, SQL-анализ, интеграции, API и безопасность студенческого контура;
- [`human-readable-code`](.agents/skills/human-readable-code/SKILL.md) — общий стандарт простого и понятного кода;
- [`csharp-readable-implementation`](.agents/skills/csharp-readable-implementation/SKILL.md) — правила реализации на C#, EF Core и CQRS;
- [`unsandboxed-codegen`](.agents/skills/unsandboxed-codegen/SKILL.md) — обязательный процесс для миграций, Swagger/OpenAPI, клиентов и другой генерации вне песочницы.

При реализации backend-фичи обычно применяются первые четыре skill. `unsandboxed-codegen` добавляется только при генерации артефактов. Явные правила этого файла имеют приоритет над общими рекомендациями skills.

## Базовые границы

- Не менять соседние репозитории без явного включения их в задачу.
- Не редактировать сгенерированные клиенты вручную.
- Не запускать генерацию в песочнице.
- Не включать чужие незакоммиченные изменения в коммит.
- Не выполнять push или deploy без явной команды пользователя.
