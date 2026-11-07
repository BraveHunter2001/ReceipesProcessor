# TASK-003 — Поэтапный рефакторинг backend

**Status:** `in-progress`  
**Owner:** разработчик  
**Created:** 2026-11-07  
**Updated:** 2026-11-07  
**Source analysis:** [`AN-003`](../analitycs/backend-refactoring.md)  
**Branch/commit:** —

## 1. Цель реализации

Повысить безопасность, тестируемость и устойчивость backend без одномоментного breaking change для текущего сайта и конвейера обработки чеков.

## 2. План

- [x] Этап 0: подготовить тестовый проект и зафиксировать API, статусы и конфигурацию без characterization-тестов.
- [x] Этап 1: внедрить options, валидацию конфигурации, `IHttpClientFactory`, timeout/retry.
- [ ] Этап 2: убрать зависимость `Application -> Infrastructure` через интерфейсы.
- [ ] Этап 3: разделить `ReceiptService` на use case-ы и DTO/projections.
- [ ] Этап 4: внедрить claim/lock, retry, `Failed` и error metadata в workers.
- [ ] Этап 5: вынести endpoint-ы из `Program.cs`, добавить validation, ProblemDetails и health checks.
- [ ] Этап 6: обновить Domain/EF mappings, индексы и миграции.
- [ ] Этап 7: добавить наблюдаемость, интеграционные тесты и провести финальную проверку.

## 3. Решения и отклонения от анализа

Рефакторинг выполнять поэтапно. Существующие endpoint-ы сохранять до отдельного решения об API versioning. Переименование `TextExctracted` выполнять только через совместимую миграцию и переходный период.

## 4. Критерии готовности

- [ ] выполнены критерии из `AN-003`;
- [ ] Application не зависит от Infrastructure;
- [ ] один чек не обрабатывается одновременно двумя workers;
- [ ] ошибка одного чека не останавливает batch и имеет ограниченный retry;
- [ ] API имеет лимиты upload, DTO и единый формат ошибок;
- [ ] миграции проверены на чистой и существующей БД;
- [ ] добавлены unit/integration/API-тесты;
- [ ] antiforgery и авторизация не забыты перед production.

## 5. Рекомендуемые точки изменений

- `ReceipesProcessor/Program.cs`
- `ReceipesProcessor/BackgroundServices/ReceiptExctractTextBackgroundService.cs`
- `ReceipesProcessor/BackgroundServices/ReceiptProccedBackgroundService.cs`
- `Application/Service/ReceiptService.cs`
- `Application/Service/TextExctractor.cs`
- `Application/Service/ReceiptTextService.cs`
- `Application/DI/DI.cs`
- `Infrastructure/StorageProvider.cs`
- `Infrastructure/PaddleOcrClient.cs`
- `Infrastructure/ApplicationContext.cs`
- `Domain/Entities/Receipt.cs`
- `Infrastructure/Migrations/`

## 6. Проверки и результат

| Проверка | Команда/способ | Результат | Дата |
|---|---|---|---|
| Backend build | `dotnet build ReceipesProcessor.slnx --no-restore` | pass, existing warnings | 2026-11-07 |
| Test project | `dotnet build Tests/ReceipesProcessor.Tests/ReceipesProcessor.Tests.csproj --no-restore` | pass; тесты будут добавлены отдельно | 2026-11-07 |
| Frontend compatibility | `npm run build` в `webclient` | pending | — |
| Migration verification | чистая и существующая PostgreSQL БД | pending | — |

## 7. Лог

### 2026-11-07 — разработчик

- **Сделано:** подготовлен поэтапный план на основе аудита backend.
- **Результат:** задача готова к планированию реализации.
- **Блокер:** открыты вопросы по авторизации, retry и совместимости статусов.

### 2026-11-07 — разработчик — этап 0 подготовка

- **Сделано:** добавлен пустой `Tests/ReceipesProcessor.Tests` в solution как место для будущих тестов.
- **Результат:** source-based characterization-тесты удалены по решению пользователя; тесты будут спроектированы и написаны отдельно.
- **Ограничение:** полноценные API/integration-тесты требуют PostgreSQL и настроек S3/MinIO; запуск host также требует внешних сервисов.
- **Проверка:** тестовый проект собирается; тестовые сценарии не запускались, поскольку в проекте пока нет тестов.

### 2026-11-07 — разработчик — этап 1: пагинация списка чеков

- **Сделано:** endpoint `GET /api/receipts` переведён на контракт `{ items, page, pageSize, totalCount, totalPages }`; добавлены значения по умолчанию `page=1`, `pageSize=10` и ограничение `pageSize<=50`.
- **Результат:** выборка списка использует `CountAsync`, `Skip` и `Take` на уровне EF Core/БД. Детализация, история и image endpoint-ы не изменялись.
- **Тесты:** автоматические тесты пагинации пока не добавлялись; будут написаны отдельно после согласования структуры тестов.
- **Блокер:** полноценные живые API-тесты с первой/второй страницей требуют PostgreSQL и инфраструктурных настроек, отсутствующих в tracked test configuration.

### 2026-11-07 — разработчик — этап 2: нормализация конфигурации и DI

- **Сделано:** удалён отдельный `ConfigurationBuilder`; host использует `builder.Configuration`. Добавлены typed settings для TelegramBot, S3, PaddleOCR, Ollama и ReceiptProcessing с сохранением legacy-ключа `botToken`.
- **Сделано:** Telegram token проверяется только при `TelegramBot:Enabled=true`; локальный запуск с выключенным ботом не требует секрета. URL, модель Ollama, timeout-ы, polling interval и batch size вынесены в конфигурацию с прежними значениями по умолчанию.
- **Сделано:** PaddleOCR и Ollama используют `IHttpClientFactory`; API-контракты внешних вызовов не изменялись.
- **Проверка:** `dotnet build ReceipesProcessor.slnx --no-restore -p:BaseOutputPath=C:\\Users\\hakdo\\AppData\\Local\\Temp\\opencode\\receipes-build\\` — pass; `git diff --check` — pass; `dotnet test Tests/ReceipesProcessor.Tests/ReceipesProcessor.Tests.csproj --no-build --no-restore -p:BaseOutputPath=...` — запуск без тестов (тестовых методов нет). Миграции не требовались, модель EF не менялась.
- **Риск:** S3 settings по-прежнему требуются при создании storage provider; это соответствует текущему поведению и не добавляет startup validation для сценариев без обращения к S3.
- **Предупреждения:** сохранены существующие предупреждения о конфликте версий `Microsoft.EntityFrameworkCore.Relational` и nullable в Telegram background service; новые nullable-предупреждения DTO PaddleOCR устранены.

## 8. Итог

Этап 0 завершён: подготовлен отдельный тестовый проект и записаны ограничения окружения. Source-based characterization-тесты удалены; полноценные тесты будут написаны отдельной задачей. До production обязательны авторизация/owner-фильтрация, возврат antiforgery и защита upload.
