# PaddleOCR GPU service

Сервис принимает PNG/JPEG в multipart-поле `file` и работает только на NVIDIA GPU.
При отсутствии CUDA, GPU или совместимых библиотек запуск завершается с ошибкой; CPU fallback отсутствует.

## Установка (Windows, Python 3.10)

1. Установите NVIDIA-драйвер и проверьте `nvidia-smi`. Для зафиксированного PaddlePaddle используется CUDA 11/cuDNN 8.
2. Создайте окружение и установите зависимости:

```powershell
cd paddleOcr
py -3.10 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

Зафиксированная матрица: `paddlepaddle-gpu==2.6.2`, `paddleocr==2.10.0`, `nvidia-cudnn-cu11==8.9.4.25`, Python 3.10.
Пакеты NVIDIA устанавливают нужные DLL в виртуальное окружение; `main.py` подключает их автоматически.
Для другой версии CUDA необходимо подобрать соответствующий официальный wheel PaddlePaddle GPU,
а не добавлять CPU-пакет `paddlepaddle`.

Диагностика:

```powershell
.\.venv\Scripts\python.exe -c "import paddle; print(paddle.__version__, paddle.is_compiled_with_cuda(), paddle.device.cuda.device_count())"
```

Ожидается `True` и количество GPU не меньше `1`.

## Запуск

```powershell
.\.venv\Scripts\python.exe main.py
```

По умолчанию слушает `0.0.0.0:8000`. Переменные: `OCR_PORT`, `OCR_HOST`, `OCR_LANGUAGE`, `OCR_MAX_FILE_SIZE`.

Проверка:

```powershell
curl http://localhost:8000/health
curl -F "file=@receipt.png;type=image/png" http://localhost:8000/ocr/receipt
```

## Запуск через Docker Compose

Требования: Linux/WSL2 или Docker Desktop с NVIDIA Container Toolkit, установленный
NVIDIA-драйвер и доступный GPU. Сервис намеренно работает только через GPU — без
CPU fallback.

Из корня репозитория:

```bash
docker compose build paddleocr
docker compose up -d paddleocr
docker compose logs -f paddleocr
```

После загрузки модели проверка готовности и запрос OCR доступны на `localhost:8000`:

```bash
curl http://localhost:8000/ready
curl -F "file=@receipt.png;type=image/png" http://localhost:8000/ocr/receipt
```

Модели сохраняются в Docker volume `receipesprocessor_paddleocr_cache`. Если GPU,
CUDA или NVIDIA Container Toolkit недоступны, контейнер не становится ready и не
переключается на CPU.
