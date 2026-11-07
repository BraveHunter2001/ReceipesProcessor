FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04

ENV DEBIAN_FRONTEND=noninteractive \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    LD_LIBRARY_PATH=/usr/local/cuda/lib64:/usr/local/cuda/targets/x86_64-linux/lib:/usr/lib/x86_64-linux-gnu

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 libgl1 libglib2.0-0 python3.10 python3-pip \
    && ln -sf /usr/lib/x86_64-linux-gnu/libcudnn.so.8 /usr/lib/x86_64-linux-gnu/libcudnn.so \
    && ln -sf /usr/local/cuda/targets/x86_64-linux/lib/libcublas.so.11 /usr/local/cuda/targets/x86_64-linux/lib/libcublas.so \
    && ln -sf /usr/local/cuda/targets/x86_64-linux/lib/libcublasLt.so.11 /usr/local/cuda/targets/x86_64-linux/lib/libcublasLt.so \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.docker.txt requirements.txt
RUN python3 -m pip install --upgrade pip \
    && python3 -m pip install -r requirements.txt

COPY main.py .

EXPOSE 8000

CMD ["python3", "main.py"]
