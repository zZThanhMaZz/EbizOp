@echo off
rem Dung EbizOp dang chay (giu nguyen cai dat). Copy/backup DB sau khi dung.
taskkill /im EbizOp.Web.exe /f >nul 2>&1
schtasks /end /tn "EbizOp" >nul 2>&1
echo EbizOp da dung.
timeout /t 2 >nul
