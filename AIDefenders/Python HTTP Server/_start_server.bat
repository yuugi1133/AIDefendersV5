@echo off
setlocal
set PYTHONUNBUFFERED=1
cd /d "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server"
title YOLO server
echo Python 3.11 venv
echo Weights: E:\Unity Projects\AIDefendersV5\AIDefenders\Assets\Datasets\grassfield_night\weights\best.pt
echo Health: http://127.0.0.1:8000/health
echo.
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -u "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\server.py" --weights "E:\Unity Projects\AIDefendersV5\AIDefenders\Assets\Datasets\grassfield_night\weights\best.pt" --host 127.0.0.1 --port 8000
if errorlevel 1 pause
exit /b %ERRORLEVEL%
