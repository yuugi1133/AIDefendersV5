@echo off
setlocal
set PYTHONUNBUFFERED=1
cd /d "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server"
title YOLO train
echo Python 3.11 venv
echo E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe
echo Dataset: grassfield_night
echo.
"E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\.venv\Scripts\python.exe" -u "E:\Unity Projects\AIDefendersV5\AIDefenders\Python HTTP Server\yolo_train.py" --data "E:/Unity Projects/AIDefendersV5/AIDefenders/Assets\Datasets\grassfield_night\dataset.yaml" --epochs 30 --batch 32 --workers 0 --dataset "grassfield_night" --result "E:/Unity Projects/AIDefendersV5/AIDefenders/Assets\Datasets\grassfield_night\train_result.json" --weights-dir "E:/Unity Projects/AIDefendersV5/AIDefenders/Assets\Datasets\grassfield_night\weights"
if errorlevel 1 goto :fail
echo.
echo Training finished.
exit /b 0
:fail
echo.
echo Training failed.
pause
exit /b 1
