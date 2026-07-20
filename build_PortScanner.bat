@echo off
setlocal enabledelayedexpansion
echo ============================================
echo   Build PortScanner (WinForms, .NET FW 4.x)
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
    echo [LOI] Khong tim thay vbc.exe. Hay chay setup_libs_PortScanner.bat truoc.
    exit /b 1
)

if not exist bin mkdir bin

"%VBC_PATH%" ^
    /target:winexe ^
    /out:bin\PortScanner.exe ^
    /optioninfer+ ^
    /optionstrict- ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Net.dll ^
    ServicePorts.vb ScanModels.vb PortScanner.vb PortScannerForm.vb Program.vb

if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai. Xem loi phia tren.
    exit /b 1
)

echo.
echo [OK] Build thanh cong: bin\PortScanner.exe
endlocal
