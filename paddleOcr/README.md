# PaddleOCR GPU service

Сервис принимает PNG/JPEG в multipart-поле `file` и работает только на NVIDIA GPU.
При отсутствии CUDA, GPU или совместимых библиотек запуск завершается с ошибкой; CPU fallback отсутствует.

## Установка (Windows, Python 3.10)

1. Установите NVIDIA-драйвер, совместимый с CUDA 12.0, и проверьте `nvidia-smi`.
2. Создайте окружение и установите зависимости:

```powershell
cd paddleOcr
py -3.10 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

Зафиксированная матрица: `paddlepaddle-gpu==2.6.2.post120`, `paddleocr==2.10.0`, Python 3.10.
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
