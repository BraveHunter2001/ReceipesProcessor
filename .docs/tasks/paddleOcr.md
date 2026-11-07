# Реализация paddleOcr

## 2026-11-07

- Создана ветка `feature/paddleocr-service` от `master`.
- Реализован FastAPI-сервис с `POST /ocr/receipt`, `GET /health` и `GET /ready`.
- Добавлены проверка MIME, лимит 20 MB, проверка фактического PNG/JPEG и единый JSON-формат ошибок.
- Модель создаётся один раз в lifespan и запускается только при доступной CUDA/GPU.
- Результат PaddleOCR нормализуется в `success/lines/text/confidence/box` и сортируется сверху вниз/слева направо.
- Зафиксирована матрица зависимостей Python 3.10 + PaddlePaddle GPU 2.6.2 CUDA 12.0 + PaddleOCR 2.10.0.
- Добавлена инструкция GPU-установки и диагностики в `paddleOcr/README.md`.

## Проверка

Локальная среда содержала `paddleocr 3.7.0`, несовместимый с `paddle 2.6.2`, и падала при инициализации (`AnalysisConfig.set_optimization_level`). После переустановки установлены совместимые версии `paddleocr 2.10.0` и `paddlepaddle-gpu 2.6.2`. Контрактные проверки endpoint выполняются с mock OCR после подмены lifespan-инициализации.

## Установка окружения

- Установлены зависимости из `paddleOcr/requirements.txt`.
- Исправлена недоступная в PyPI строка `paddlepaddle-gpu==2.6.2.post120` на доступную `paddlepaddle-gpu==2.6.2`.
- Проверено: `paddle==2.6.2`, CUDA-сборка — `True`, GPU — `1` (`NVIDIA GeForce GTX 1660 Ti`).
- Проверено: `paddleocr==2.10.0`, `fastapi==0.115.6`.
- `pip check` завершился без конфликтов зависимостей.
