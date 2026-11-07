# ReceipesProcessor

Документация проекта для получения детализации чеков. Система принимает фотографию чека через Telegram, распознаёт текст, преобразует сырой OCR-текст в структурированный JSON с помощью локальной LLM и сохраняет результат в PostgreSQL.

> Название репозитория и классов содержит написание `Receipes`. В документации сохранено фактическое имя проекта, чтобы команды и пути соответствовали исходному коду.

## Содержание

- [Назначение](#назначение)
- [Архитектура](#архитектура)
- [Жизненный цикл чека](#жизненный-цикл-чека)
- [Структура проекта](#структура-проекта)
- [Требования](#требования)
- [Конфигурация](#конфигурация)
- [Запуск зависимостей](#запуск-зависимостей)
- [Запуск приложения](#запуск-приложения)
- [Использование Telegram-бота](#использование-telegram-бота)
- [HTTP API](#http-api)
- [Модель данных](#модель-данных)
- [OCR-сервис](#ocr-сервис)
- [Локальная LLM](#локальная-llm)
- [Логирование и диагностика](#логирование-и-диагностика)
- [Разработка](#разработка)
- [Известные ограничения](#известные-ограничения)

## Назначение

Проект автоматизирует обработку фотографий кассовых чеков:

1. Пользователь отправляет фото Telegram-боту.
2. Бот скачивает изображение и сохраняет его в S3-совместимое хранилище MinIO.
3. Фоновый обработчик передаёт изображение в OCR-сервис.
4. Распознанный текст сохраняется в PostgreSQL.
5. Другой фоновый обработчик отправляет текст в локальную LLM через Ollama.
6. Ответ LLM десериализуется в структуру чека и сохраняется в PostgreSQL.
7. Готовые чеки можно получить командой `/receipts` или через веб-интерфейс/API.

## Архитектура

```text
Telegram
   │ фотография
   ▼
ReceipesProcessor (.NET)
   ├── TelegramBotBackgroundService
   │       └── сохраняет изображение в MinIO и создаёт Receipt
   ├── ReceiptExctractTextBackgroundService
   │       └── получает изображение → OpenCV → OCR HTTP API
   ├── ReceiptProccedBackgroundService
   │       └── RawText → Ollama → ReceiptDto → сущности БД
   ├── ASP.NET Core API + React/Vite frontend
   └── PostgreSQL (состояние и результаты)

OCR-сервис (Python/FastAPI/PaddleOCR)
   ▲
   └── POST /ocr/receipt (multipart-поле `file`)

Ollama
   ▲
   └── POST http://localhost:11434/api/generate
```

Основное приложение является ASP.NET Core Web-приложением на .NET 10. Фоновые задачи запускаются как `BackgroundService` и проверяют очередь чеков примерно раз в секунду. За один проход каждый обработчик берёт не более 50 записей.

## Жизненный цикл чека

Статусы определены в `Domain/Entities/Receipt.cs`:

| Статус | Значение |
|---|---|
| `Created` | Изображение загружено, чек создан, OCR ещё не выполнен. |
| `TextExctracted` | OCR завершён, сырой текст сохранён в `RawText`. |
| `Finished` | LLM разобрала текст, итоговая структура сохранена. |

При переходе между статусами создаётся запись в `ReceiptHistories` с временем изменения. При ошибке обработчик пишет ошибку в лог; отдельный статус ошибки в текущей модели не предусмотрен.

## Структура проекта

```text
ReceipesProcessor/
├── ReceipesProcessor/                 # ASP.NET Core host, Telegram и фоновые службы
│   ├── BackgroundServices/
│   ├── Program.cs
│   └── appsettings.json
├── Domain/                             # доменные сущности
│   └── Entities/
├── Application/                        # сценарии и DTO
│   ├── Service/
│   ├── Models/
│   └── tessdata/                       # языковые данные Tesseract
├── Infrastructure/                     # PostgreSQL, S3/MinIO, OCR HTTP client
│   ├── Migrations/
│   ├── ApplicationContext.cs
│   └── PaddleOcrClient.cs
├── paddleOcr/                          # Python OCR-сервис и зависимости
├── webclient/                           # React + TypeScript + Vite интерфейс
├── docker-compose.yml                  # PostgreSQL и MinIO
└── .docs/                              # документация
```

## Требования

- .NET SDK 10;
- Docker и Docker Compose;
- Node.js и npm — для разработки веб-клиента;
- Ollama;
- Python 3.10+ для OCR-сервиса;
- GPU и совместимая CUDA-сборка PaddlePaddle — если OCR запускается на GPU.

## Конфигурация

`ReceipesProcessor/Program.cs` требует следующие настройки:

- `botToken` — токен Telegram-бота;
- `ConnectionStrings:DefaultConnection` — строка подключения к PostgreSQL;
- `TesseractSettings:FolderName` — папка с tessdata, по умолчанию `tessdata`;
- `S3Settings:ServiceUrl` — URL MinIO/S3;
- `S3Settings:AccessKey` и `S3Settings:SecretKey`;
- `S3Settings:BucketName`.

Пример `ReceipesProcessor/appsettings.Development.json`:

```json
{
  "botToken": "TELEGRAM_BOT_TOKEN",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ReceipesProcessorDb;Username=postgres;Password=adminadmin"
  },
  "TesseractSettings": {
    "FolderName": "tessdata"
  },
  "S3Settings": {
    "ServiceUrl": "http://localhost:9000",
    "AccessKey": "minio_admin",
    "SecretKey": "minio_secure_password",
    "BucketName": "receipts"
  }
}
```

> Файл с реальными секретами не следует коммитить. Для локальной разработки можно использовать User Secrets или переменные окружения. Значения PostgreSQL и MinIO должны совпадать с `docker-compose.yml`; имя bucket при необходимости нужно создать в MinIO.

В текущем `appsettings.json` уже настроено консольное логирование Serilog и путь `tessdata`, но секреты и подключения к БД/S3 туда не добавлены.

## Запуск зависимостей

Из корня репозитория:

```bash
docker compose up -d
```

Компоненты:

| Компонент | Адрес | Назначение |
|---|---:|---|
| PostgreSQL 16 | `localhost:5432` | База данных чеков |
| MinIO API | `localhost:9000` | S3-совместимое хранение изображений |
| MinIO Console | `http://localhost:9001` | Веб-консоль MinIO |

Учётные данные из Compose-файла:

- PostgreSQL: `postgres` / `adminadmin`, база `ReceipesProcessorDb`;
- MinIO: `minio_admin` / `minio_secure_password`.

После запуска MinIO создайте bucket `receipts` через консоль или S3-клиент.

## Запуск приложения

1. Настройте `appsettings.Development.json` или User Secrets.
2. Убедитесь, что запущены OCR-сервис и Ollama.
3. Примените миграции:

   ```bash
   dotnet ef database update --project Infrastructure --startup-project ReceipesProcessor
   ```

4. Запустите приложение:

   ```bash
   dotnet run --project ReceipesProcessor
   ```

ASP.NET Core раздаёт собранный frontend как статические файлы. Для разработки frontend отдельно:

```bash
cd webclient
npm install
npm run dev
```

Для production-сборки frontend:

```bash
cd webclient
npm run build
```

При публикации backend MSBuild также выполняет `npm install` и `npm run build` согласно target `PublishRunWebpack` в `ReceipesProcessor.csproj`.

## Использование Telegram-бота

### Загрузка чека

Отправьте боту фотографию чека. Бот:

1. выбирает самое крупное доступное разрешение фотографии;
2. скачивает файл из Telegram;
3. сохраняет его в MinIO под ключом `<telegram-file-id>.png`;
4. создаёт запись чека со статусом `Created`;
5. отвечает идентификатором созданного чека.

### Получение готовых чеков

Отправьте команду:

```text
/receipts
```

Бот отправит все чеки пользователя, которые находятся в статусе `Finished`, с товарами, количеством, ценой, суммой позиции и итогом.

Текущая реализация запроса `/receipts` выбирает все завершённые чеки без фильтра по `OwnerKey`. Если бот используется несколькими пользователями, это следует учитывать как ограничение безопасности и исправить фильтрацию по `Chat.Id`.

## HTTP API

### `GET /api/receipts`

Возвращает список чеков для веб-интерфейса. Endpoint реализован в `ReceipesProcessor/Program.cs` и вызывает `ReceiptService.GetList()`.

Пример ответа:

```json
[
  {
    "id": 1,
    "totalSum": 1250.5,
    "currency": "RUB",
    "status": "Finished",
    "items": [
      {
        "id": 1,
        "name": "Молоко",
        "quantity": 2,
        "price": 100.0,
        "sum": 200.0
      }
    ]
  }
]
```

Точная форма JSON зависит от DTO и настроек сериализации ASP.NET Core. В текущей реализации endpoint не принимает фильтры, пагинацию или авторизацию.

## Модель данных

### `Receipt`

| Поле | Тип | Описание |
|---|---|---|
| `Id` | `int` | Идентификатор чека. |
| `TotalSum` | `decimal?` | Итоговая сумма. |
| `Currency` | `string` | Валюта, по умолчанию `RUB`. |
| `Items` | `List<ReceiptItem>?` | Товары или услуги. |
| `Status` | `ReceiptStatus` | Текущий этап обработки. В БД хранится строкой. |
| `RawText` | `string?` | Текст, возвращённый OCR. |
| `ImageId` | `string?` | Ключ изображения в S3/MinIO. |
| `OwnerKey` | `string` | Идентификатор владельца; сейчас используется `Chat.Id`. |

### `ReceiptItem`

| Поле | Тип | Описание |
|---|---|---|
| `Name` | `string` | Название позиции. |
| `Quantity` | `double` | Количество. |
| `Price` | `decimal` | Цена за единицу. |
| `Sum` | `decimal` | Сумма позиции. |
| `ReceiptId` | `int` | Внешний ключ на чек. |

### `ReceiptHistory`

Хранит историю переходов чека: `ReceiptId`, `Status` и `AtRecord` в UTC.

## OCR-сервис

Класс `Infrastructure/PaddleOcrClient` отправляет изображение HTTP-запросом:

```http
POST http://localhost:8000/ocr/receipt
Content-Type: multipart/form-data
```

Имя multipart-поля — `file`. Ожидаемый ответ:

```json
{
  "success": true,
  "lines": [
    {
      "text": "МОЛОКО 2 x 100.00",
      "confidence": 0.98,
      "box": [[10, 10], [300, 10], [300, 40], [10, 40]]
    }
  ]
}
```

Основное приложение собирает значения `lines[*].text` в единый текст, разделяя строки переводом строки.

Перед отправкой в PaddleOCR `TextExctractor` выполняет обработку изображения через OpenCV:

1. перевод в оттенки серого;
2. увеличение в 2 раза кубической интерполяцией;
3. Gaussian Blur;
4. адаптивная бинаризация;
5. кодирование результата в PNG.

В проекте также присутствуют языковые данные Tesseract (`rus` и `eng`), но текущий путь `Exctract` использует PaddleOCR HTTP-клиент. Метод Tesseract остаётся вспомогательным и напрямую не вызывается.

### Установка Python-зависимостей

```bash
cd paddleOcr
python -m venv .venv
python -m pip install -r requirements.txt
```

Для GPU необходимо установить совместимые версии CUDA, PaddlePaddle и PaddleOCR. Для CPU-варианта замените GPU-пакет на подходящий CPU-пакет PaddlePaddle.

> В текущем репозитории `paddleOcr/main.py` только проверяет CUDA и создаёт объект `PaddleOCR`. Он не запускает FastAPI и не реализует `/ocr/receipt`. Перед полноценным запуском нужно добавить endpoint, вызывающий OCR, и запустить его, например через `uvicorn`. Это обязательная доработка для работы .NET-пайплайна.

## Локальная LLM

`Application/Service/ReceiptTextService` обращается к Ollama:

```http
POST http://localhost:11434/api/generate
Content-Type: application/json
```

Параметры запроса:

```json
{
  "model": "qwen2.5:3b",
  "prompt": "...сырой текст чека...",
  "stream": false,
  "options": { "temperature": 0.0 },
  "format": "json"
}
```

Подготовка Ollama:

```bash
ollama serve
ollama pull qwen2.5:3b
```

Из поля `response` ответа Ollama извлекается JSON следующей формы:

```json
{
  "total_sum": 1250.50,
  "currency": "RUB",
  "items": [
    {
      "name": "Молоко",
      "quantity": 2,
      "price": 100.00,
      "sum": 200.00
    }
  ]
}
```

Если количество не указано, prompt требует считать его равным `1`; если валюта не распознана, используется `RUB`. После десериализации данные переносятся из `ReceiptDto` в `Receipt` и `ReceiptItem`.

## Логирование и диагностика

Логи выводятся в консоль через Serilog. Полезные сообщения:

- запуск Telegram background service;
- начало сканирования чеков в статусах `Created` и `TextExctracted`;
- размер загруженной batch-порции;
- завершение OCR или LLM-обработки для конкретного чека;
- ошибки фоновых задач и ошибки Telegram API.

Если чек не обрабатывается, проверьте по порядку:

1. доступность PostgreSQL и применённые миграции;
2. наличие bucket и доступность MinIO;
3. корректность `botToken`;
4. доступность OCR по `http://localhost:8000/ocr/receipt`;
5. доступность Ollama по `http://localhost:11434` и наличие модели `qwen2.5:3b`;
6. статус записи в таблице `Receipts` и значение `RawText`;
7. консольные логи приложения.

## Разработка

Слои проекта разделены следующим образом:

- `Domain` — сущности и enum статусов;
- `Application` — DTO и прикладные сервисы OCR/LLM;
- `Infrastructure` — EF Core, PostgreSQL, MinIO и внешние HTTP-интеграции;
- `ReceipesProcessor` — host, Telegram polling, фоновые обработчики и HTTP API;
- `webclient` — интерфейс просмотра результатов.

После изменения модели данных создайте миграцию из корня репозитория:

```bash
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project ReceipesProcessor
dotnet ef database update --project Infrastructure --startup-project ReceipesProcessor
```

Проверка frontend:

```bash
cd webclient
npm run build
npm run lint
```

## Известные ограничения

- В репозитории нет готовой конфигурации со значениями `botToken`, подключения к PostgreSQL и `S3Settings`; её нужно создать локально.
- Python OCR-заготовка не содержит ожидаемый FastAPI endpoint `/ocr/receipt`.
- URL OCR и Ollama зашиты в исходном коде и не вынесены в конфигурацию.
- Фоновые обработчики запускаются с интервалом около одной секунды, но не используют отдельную блокировку или claim-механику; при горизонтальном масштабировании возможна повторная обработка записей.
- Ошибка обработки не переводит чек в отдельный error-статус и не реализует автоматический retry policy.
- `/api/receipts` не имеет авторизации и возвращает общий список.
- Команда `/receipts` не фильтрует записи по владельцу и потенциально может показать чеки других пользователей.
- Изображение сохраняется в S3, но удаление и политика хранения не реализованы.
- В `TextExctractor` используется `MemoryStream.GetBuffer()`, поэтому для некоторых потоков с недоступным буфером безопаснее перейти на `ToArray()`.
- Формат и качество результата зависят от OCR и LLM; перед использованием финансовых данных рекомендуется проверка суммы и позиций.
