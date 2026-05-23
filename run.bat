@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo   ArchiStyler
echo ========================================
echo.

dotnet run --project "src\ArchiStyler\ArchiStyler.csproj"
set EXITCODE=%ERRORLEVEL%

echo.
if %EXITCODE% neq 0 (
    echo Ошибка запуска. Код: %EXITCODE%
    if exist "src\ArchiStyler\bin\Debug\net8.0\crash.log" (
        echo.
        echo --- crash.log ---
        type "src\ArchiStyler\bin\Debug\net8.0\crash.log"
    )
) else (
    echo Приложение завершено.
)
echo.
pause
