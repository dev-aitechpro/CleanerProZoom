@echo off
chcp 65001 >nul
title Очистка Cleaner Pro

echo ========================================
echo    ОЧИСТКА ПРОЕКТА CLEANER PRO
echo ========================================
echo.

echo [1/5] Закрытие Visual Studio...
taskkill /f /im devenv.exe 2>nul
echo.

echo [2/5] Удаление папки bin...
if exist "bin" (rmdir /s /q bin & echo ✅ bin удалена) else (echo ⚠️ bin не найдена)

echo [3/5] Удаление папки obj...
if exist "obj" (rmdir /s /q obj & echo ✅ obj удалена) else (echo ⚠️ obj не найдена)

echo [4/5] Удаление папки .vs...
if exist ".vs" (rmdir /s /q .vs & echo ✅ .vs удалена) else (echo ⚠️ .vs не найдена)

echo [5/5] Очистка кэша .NET...
dotnet clean 2>nul

echo.
echo ========================================
echo    ✅ ОЧИСТКА ЗАВЕРШЕНА!
echo ========================================
echo.
echo Теперь откройте проект и соберите его заново.
pause