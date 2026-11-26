@echo off
rem Go EbizOp: task + firewall rule. Du lieu (db/backups/license) HOI rieng, mac dinh GIU.
setlocal EnableDelayedExpansion
title EbizOp uninstall
cd /d "%~dp0"
net session >nul 2>&1
if errorlevel 1 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

schtasks /end /tn "EbizOp" >nul 2>&1
taskkill /im EbizOp.Web.exe /f >nul 2>&1
schtasks /delete /tn "EbizOp" /f
netsh advfirewall firewall delete rule name="EbizOp"

set /p "DEL=Xoa ca du lieu (ebizop.db, backups, license)? [y/N]: "
if /i "!DEL!"=="y" (
  del /q "%~dp0ebizop.db*" 2>nul
  rd /s /q "%~dp0backups"
  rd /s /q "%~dp0license"
  echo Da xoa du lieu.
)
echo.
echo Da go. File trong folder (exe, db neu giu) tu tay xoa sau khi backup.
pause
