# Реализация paddleOcr

**ID:** `TASK-001`<br>
**Status:** `done`<br>
**Owner:** разработчик<br>
**Created:** 2026-11-07<br>
**Updated:** 2026-11-07<br>
**Source analysis:** [`AN-001`](../analitycs/paddleOcr.md)

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

## Проверка на изображении из `.docs/attachments`

- Изображение `photo_2026-11-07_11-14-51.jpg` корректно читается: JPEG, `696x1280`, RGB.
- Модели PaddleOCR успешно скачались и были созданы с `use_gpu=True`; Paddle выбрал GPU 0.
- Реальный inference остановился с ошибкой `cudnn64_8.dll ... error code is 126`.
- На машине найден CUDA Toolkit 11.8, но cuDNN DLL отсутствуют в `CUDA\\v11.8\\bin` и в окружении Paddle.
- Для завершения проверки необходимо установить совместимый cuDNN 8.x для CUDA 11.8 и добавить его `bin` в `PATH`.

## Установка cuDNN и реальный inference

- Установлены `nvidia-cudnn-cu11==8.9.4.25`, `nvidia-cublas-cu11==11.11.3.6` и `nvidia-cuda-nvrtc-cu11==11.8.89` в `paddleOcr/venv`.
- DLL-пути подключены в `main.py` автоматически перед импортом PaddlePaddle.
- Реальная проверка на `photo_2026-11-07_11-14-51.jpg` успешно выполнена на GPU: PaddleOCR вернул текстовые блоки, confidence и координаты.

- Добавлен `paddleOcr/Dockerfile` на базе NVIDIA CUDA 11.8/cuDNN 8 и отдельный `requirements.docker.txt`.
- В `docker-compose.yml` добавлен GPU-сервис `paddleocr` с публикацией `localhost:8000`, health-check и volume для кэша моделей.
