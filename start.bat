@echo off
rem Chay EbizOp co cua so console (xem log truc tiep). Ctrl+C de dung.
rem EnableDelayedExpansion: bien LANIP gan trong vong for, doc bang !LANIP!
setlocal EnableDelayedExpansion
set "EBIZ_PORTABLE=1"
set "EBIZ_DEFAULT_PIN=0000"
set "PORT=5388"
if exist "%~dp0port.txt" set /p PORT=<"%~dp0port.txt"
cd /d "%~dp0"
rem IP LAN cua may chu - hien cho nguoi dung thay vi localhost
rem ipconfig + parse bang cmd - khong goi powershell (for /f nuot $_ duong dan)
set "LANIP="
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
  set "CAND=%%a"
  set "CAND=!CAND: =!"
  if not "!CAND:~0,3!"=="127" if not "!CAND:~0,4!"=="169." if not defined LANIP set "LANIP=!CAND!"
)
if not defined LANIP (
  echo KHONG tu do duoc IP LAN.
  set /p "LANIP=Nhap IP LAN may chu (vd 192.168.1.10): "
)
echo EbizOp http://!LANIP!:%PORT%  (Ctrl+C de dung)
start "" "http://!LANIP!:%PORT%"
"%~dp0EbizOp.Web.exe" --urls http://0.0.0.0:%PORT%
