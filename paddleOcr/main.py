import paddle
from paddleocr import PaddleOCR

# 1. Проверяем, видит ли paddle вашу CUDA-сборку
print("CUDA доступна:", paddle.is_compiled_with_cuda())

# 2. Инициализируем OCR с явным указанием GPU
ocr = PaddleOCR(device="gpu", lang="ru")
print("PaddleOCR успешно инициализирован на GPU!")