@echo off
setlocal enabledelayedexpansion
echo ============================================
echo   Kiem tra moi truong build - PortScanner
echo ============================================

set VBC_PATH=

for %%F in (
    "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe"
    "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\vbc.exe"
) do (
    if exist %%F (
        set VBC_PATH=%%~F
    )
)

if "%VBC_PATH%"=="" (
    echo [LOI] Khong tim thay vbc.exe cua .NET Framework 4.x tren may nay.
    echo       Vui long cai dat .NET Framework 4.x Developer Pack.
    exit /b 1
)

echo [OK] Tim thay vbc.exe tai: %VBC_PATH%
echo [OK] Du an nay chi dung thu vien chuan (System, System.Windows.Forms, System.Drawing^)
echo      nen khong can cai them goi NuGet nao.
echo.
echo Hoan tat kiem tra. Co the chay build_PortScanner.bat de bien dich.

endlocal
