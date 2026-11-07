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

Локальная среда содержит `paddle 2.6.2` с CUDA, но установленный `paddleocr 3.7.0` несовместим с ним и падает при инициализации (`AnalysisConfig.set_optimization_level`). Полный inference до переустановки зависимостей не выполнялся. Проверены версии Python и наличие одного CUDA GPU. Контрактные проверки endpoint выполняются с mock OCR после подмены lifespan-инициализации.
