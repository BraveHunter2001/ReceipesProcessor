"""GPU-only PaddleOCR HTTP service."""

from __future__ import annotations

import logging
import os
import time
from contextlib import asynccontextmanager
from typing import Any

import numpy as np
import paddle
from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.responses import JSONResponse
from PIL import Image, UnidentifiedImageError
from paddleocr import PaddleOCR

logging.basicConfig(level=os.getenv("LOG_LEVEL", "INFO"))
logger = logging.getLogger("paddleocr-service")

MAX_FILE_SIZE = int(os.getenv("OCR_MAX_FILE_SIZE", str(20 * 1024 * 1024)))
LANGUAGE = os.getenv("OCR_LANGUAGE", "ru")
HOST = os.getenv("OCR_HOST", "0.0.0.0")
PORT = int(os.getenv("OCR_PORT", "8000"))
SUPPORTED_MIME_TYPES = {"image/png", "image/jpeg"}

ocr_engine: PaddleOCR | None = None


def error_response(status_code: int, code: str, message: str) -> JSONResponse:
    return JSONResponse(
        status_code=status_code,
        content={"success": False, "error": {"code": code, "message": message}},
    )


def ensure_gpu() -> None:
    if not paddle.is_compiled_with_cuda():
        raise RuntimeError("PaddlePaddle is not compiled with CUDA; GPU-only service cannot start")
    if paddle.device.cuda.device_count() < 1:
        raise RuntimeError("No CUDA GPU is available; GPU-only service cannot start")


def create_ocr_engine() -> PaddleOCR:
    ensure_gpu()
    # PaddleOCR 2.x uses use_gpu. CPU fallback is intentionally disabled.
    return PaddleOCR(lang=LANGUAGE, use_gpu=True, use_angle_cls=True)


@asynccontextmanager
async def lifespan(_: FastAPI):
    global ocr_engine
    logger.info("Starting PaddleOCR: python=%s paddle=%s language=%s", os.sys.version.split()[0], paddle.__version__, LANGUAGE)
    try:
        ocr_engine = create_ocr_engine()
    except Exception as exc:
        logger.critical("GPU OCR initialization failed: %s", exc)
        raise RuntimeError(f"GPU OCR initialization failed: {exc}") from exc
    logger.info("PaddleOCR model initialized on GPU")
    yield
    ocr_engine = None
    logger.info("PaddleOCR service stopped")


app = FastAPI(title="Receipt PaddleOCR Service", lifespan=lifespan)


@app.get("/health")
async def health() -> dict[str, Any]:
    return {"status": "ok" if ocr_engine is not None else "starting", "model_ready": ocr_engine is not None, "device": "gpu"}


@app.get("/ready")
async def ready() -> dict[str, Any]:
    if ocr_engine is None:
        return error_response(503, "MODEL_NOT_READY", "OCR model is not ready")
    return {"status": "ready", "model_ready": True, "device": "gpu"}


async def read_upload(file: UploadFile) -> bytes:
    if not file.filename:
        raise HTTPException(status_code=400, detail={"code": "EMPTY_FILE", "message": "A file is required"})
    if file.content_type not in SUPPORTED_MIME_TYPES:
        raise HTTPException(status_code=415, detail={"code": "UNSUPPORTED_MEDIA_TYPE", "message": "Only PNG and JPEG images are supported"})

    chunks: list[bytes] = []
    size = 0
    while True:
        chunk = await file.read(1024 * 1024)
        if not chunk:
            break
        size += len(chunk)
        if size > MAX_FILE_SIZE:
            raise HTTPException(status_code=413, detail={"code": "FILE_TOO_LARGE", "message": "The uploaded file is too large"})
        chunks.append(chunk)
    data = b"".join(chunks)
    if not data:
        raise HTTPException(status_code=400, detail={"code": "EMPTY_FILE", "message": "The uploaded file is empty"})
    return data


def decode_image(data: bytes) -> np.ndarray:
    try:
        with Image.open(__import__("io").BytesIO(data)) as image:
            image.verify()
        with Image.open(__import__("io").BytesIO(data)) as image:
            return np.asarray(image.convert("RGB"))
    except (UnidentifiedImageError, OSError, ValueError) as exc:
        raise HTTPException(status_code=422, detail={"code": "INVALID_IMAGE", "message": "The uploaded file is not a valid image"}) from exc


def point(value: Any) -> list[int]:
    return [int(round(float(value[0]))), int(round(float(value[1])))]


def normalize_result(result: Any) -> list[dict[str, Any]]:
    """Normalize PaddleOCR 2.x output: [[box, (text, confidence)], ...]."""
    lines: list[dict[str, Any]] = []
    if (
        result
        and isinstance(result, list)
        and isinstance(result[0], list)
        and not (
            len(result[0]) >= 2
            and isinstance(result[0][1], (tuple, list))
            and len(result[0][1]) >= 2
            and isinstance(result[0][1][0], str)
        )
    ):
        result = result[0]
    for item in result or []:
        try:
            box, recognition = item[0], item[1]
            text, confidence = recognition[0], max(0.0, min(1.0, float(recognition[1])))
            polygon = [point(p) for p in box]
            if len(polygon) == 4 and str(text).strip():
                lines.append({"text": str(text), "confidence": confidence, "box": polygon})
        except (IndexError, KeyError, TypeError, ValueError):
            logger.warning("Ignoring malformed OCR result item")
    lines.sort(key=lambda line: (min(p[1] for p in line["box"]), min(p[0] for p in line["box"])))
    return lines


@app.post("/ocr/receipt", response_model=None)
async def recognize_receipt(file: UploadFile = File(...)) -> dict[str, Any] | JSONResponse:
    if ocr_engine is None:
        return error_response(503, "MODEL_NOT_READY", "OCR model is not ready")
    started = time.perf_counter()
    try:
        data = await read_upload(file)
        image = decode_image(data)
        result = ocr_engine.ocr(image, cls=True)
        lines = normalize_result(result)
        logger.info("OCR completed: size=%d mime=%s lines=%d elapsed_ms=%.1f", len(data), file.content_type, len(lines), (time.perf_counter() - started) * 1000)
        return {"success": True, "lines": lines}
    except HTTPException as exc:
        detail = exc.detail if isinstance(exc.detail, dict) else {"code": "INVALID_REQUEST", "message": str(exc.detail)}
        return error_response(exc.status_code, detail["code"], detail["message"])
    except Exception:
        logger.exception("OCR inference failed")
        return error_response(500, "OCR_ERROR", "OCR processing failed")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host=HOST, port=PORT)
