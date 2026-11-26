@echo off
rem EbizOp installer (portable) - chay lan dau hoi port, dang ky tu chay cung Windows,
rem mo firewall cho LAN. Phai chay tu tai khoan Windows ban dung hang ngay (task ONLOGON
rem theo user; license DPAPI gan voi user nay).
setlocal EnableDelayedExpansion
title EbizOp install

rem --- duoc goi sau khi elevate: %~dp0 = thu muc cua CHINH file bat nay (dung ca khi
rem working dir la system32, path co dau cach; %CD%/%1 sau elevate khong tin duoc) ---
cd /d "%~dp0"

rem --- can quyen admin (netsh + schtasks). KHONG tu elevate bang powershell:
rem elevate + exit lam cua so goc tat, cua so elevated chay rieng, khach khong
rem theo doi duoc loi. Yeu cau right-click Run as administrator, bao ro neu thieu.
net session >nul 2>&1
if errorlevel 1 (
  echo CAN QUYEN ADMIN de dang ky Scheduled Task + mo firewall.
  echo Chuot phai vao install.bat -^> "Run as administrator" roi cai lai.
  pause
  exit /b 1
)

set "DIR=%CD%"
set "PORT="
if exist "%DIR%\port.txt" set /p PORT=<"%DIR%\port.txt"
:askport
if not defined PORT set "PORT=5388"
set /p "ANSW=Port lan [!PORT!] (Enter = giu nguyen): "
if not "!ANSW!"=="" set "PORT=!ANSW!"
echo !PORT!| findstr /r "^[0-9][0-9]*$" >nul
if errorlevel 1 (
  echo Port phai la so.
  goto askport
)

rem --- port dang bo chan? ---
rem /c: = ca cum la MOT regex. Thieu /c: thi findstr /r tach theo khoang trang thanh
rem 2 pattern OR, ".*LISTENING" khop moi dong listening -> bao busy oan moi port.
netstat -ano | findstr /r /c:":!PORT![ ]" | findstr /i "LISTENING" >nul
if not errorlevel 1 (
  echo Port !PORT! dang duoc su dung. Chon port khac.
  set "PORT="
  goto askport
)
> "%DIR%\port.txt" echo !PORT!

rem --- IP LAN cua may chu (hien cho nguoi cai thay vi localhost) ---
rem ipconfig + parse bang cmd - khong goi powershell (for /f nuot $_ duong dan)
set "LANIP="
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
  set "CAND=%%a"
  set "CAND=!CAND: =!"
  if not "!CAND:~0,3!"=="127" if not "!CAND:~0,4!"=="169." if not defined LANIP set "LANIP=!CAND!"
)
if not defined LANIP (
  echo KHONG tu do duoc IP LAN. Kiem tra cap mang/WiFi.
  set /p "LANIP=Nhap IP LAN may chu (vd 192.168.1.10): "
)

rem --- task tu chay khi dang nhap (hidden qua wscript) ---
rem schtasks /tr KHONG chiu duong dan co khoang trang (du co quote): bao
rem "Invalid argument/option" tren may cai vao folder co space. Fix: doi sang
rem short path 8.3 (khong space) truoc khi truyen. Task cu hong/ket thi end +
rem xoa truoc (khong co thi bo qua). LUU Y: dung !DIR! (delayed) trong block
rem parentheses, KHONG dung %DIR% ? %DIR% expand truoc parse ? ngoac trong
rem ten folder (vd "EbizOp (1)") vao parse ? block vo + cmd tat ngay.
schtasks /end /tn "EbizOp" >nul 2>&1
schtasks /delete /tn "EbizOp" /f >nul 2>&1
set "TASKLOG=%DIR%\install-task.log"
set "VBS="
for %%I in ("%DIR%\start-hidden.vbs") do set "VBS=%%~sI"
set "VBSERR=0"
echo !VBS!| findstr /c:" " >nul
set "FIND-EXIT=!ERRORLEVEL!"
if "!FIND-EXIT!"=="0" set "VBSERR=1"
if "!VBSERR!"=="1" goto novbs
goto vbsok
:novbs
echo Folder cai dat co khoang trang ma Windows khong tao duoc short path 8.3:
echo   !DIR!
echo Copy folder EbizOp sang duong dan khong space (vd D:\EbizOp^) roi cai lai.
pause
exit /b 1
:vbsok
set "CRES=0"
schtasks /create /tn "EbizOp" /sc ONLOGON /tr "wscript.exe !VBS!" /rl LIMITED /f > "!TASKLOG!" 2>&1
set "CREATE-EXIT=!ERRORLEVEL!"
schtasks /query /tn "EbizOp" >nul 2>&1
set "QUERY-EXIT=!ERRORLEVEL!"
if not "!QUERY-EXIT!"=="0" set "CRES=1"
if not "!CRES!"=="1" goto createok
echo Dang ky Scheduled Task THAT BAI. Chi tiet loi tu schtasks:
echo ----------------------------------------
type "!TASKLOG!"
echo ----------------------------------------
echo Log giu lai: !TASKLOG! (gui file nay khi bao loi)
echo Thong tin may:
whoami
echo DIR=!DIR!
echo CREATE-EXIT=!CREATE-EXIT! QUERY-EXIT=!QUERY-EXIT! CRES=!CRES!
sc query schedule | findstr STATE
schtasks /query /tn "EbizOp" 2>&1
pause
exit /b 1
:createok
del "%TASKLOG%" 2>nul

rem --- firewall: 1 rule ten dinh danh, xoa truoc khi them de khong trung ---
netsh advfirewall firewall delete rule name="EbizOp" >nul 2>&1
netsh advfirewall firewall add rule name="EbizOp" dir=in action=allow protocol=TCP localport=!PORT! profile=private

rem --- chay ngay (verify process that, khong tin exit code /run) ---
schtasks /run /tn "EbizOp" >nul 2>&1
echo Dang khoi dong EbizOp tai http://!LANIP!:!PORT! ...
set "OK="
for /l %%i in (1,1,30) do (
  if not defined OK (
    curl -s -o nul "http://localhost:!PORT!/login" && set "OK=1"
    if not defined OK timeout /t 1 /nobreak >nul
  )
)
if not defined OK echo CANH BAO: app chua len sau 30s. Chay start.bat de xem loi.

echo.
echo ============================
echo  EbizOp cai dat XONG
echo  May chu: http://!LANIP!:!PORT!
echo  (healthcheck noi bo van dung localhost)
echo  Go key ban quyen o man hinh kich hoat (lien he nguoi ban de duoc cap key^).
echo  Go uninstall.bat de go.
echo ============================
start "" "http://!LANIP!:!PORT!"
pause