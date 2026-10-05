@echo off
setlocal
cd /d "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server"
title YOLO library setup
echo Python 3.11 virtualenv / library setup
echo GPU: NVIDIA GeForce GTX 1060 6GB
echo Torch: cu126
echo.
if exist "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" goto :pip
echo Creating virtualenv...
"C:\Users\4gihy\AppData\Local\Programs\Python\Python311\python.exe" -m venv "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv"
if errorlevel 1 goto :fail
:pip
echo Upgrading pip...
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -m pip install --upgrade pip
if errorlevel 1 goto :fail
echo Removing previous torch if present...
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -m pip uninstall -y torch torchvision torchaudio
echo Installing CUDA PyTorch (cu126)...
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -m pip install torch torchvision --index-url https://download.pytorch.org/whl/cu126
if errorlevel 1 (
  echo CUDA torch install failed. Falling back to CPU torch.
  "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -m pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu
  if errorlevel 1 goto :fail
)
echo Installing requirements...
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -m pip install -r "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\requirements.txt"
if errorlevel 1 goto :fail
echo.
echo Checking torch.cuda...
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -c "import torch; print('torch', torch.__version__); print('cuda', torch.cuda.is_available()); print('device', torch.cuda.get_device_name(0) if torch.cuda.is_available() else 'cpu')"
echo.
echo Setup finished.
pause
exit /b 0
:fail
echo.
echo Setup failed.
pause
exit /b 1
