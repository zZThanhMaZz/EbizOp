' EbizOp - launcher khong cua so cho Scheduled Task (ONLOGON).
' Doc cong tu port.txt, set EBIZ_PORTABLE de exe neo moi thu theo thu muc cua no.
Dim fso, sh, dir, port, f
dir = Left(WScript.ScriptFullName, InStrRev(WScript.ScriptFullName, "\") - 1)
port = "5388"
Set fso = CreateObject("Scripting.FileSystemObject")
If fso.FileExists(dir & "\port.txt") Then
  Set f = fso.OpenTextFile(dir & "\port.txt", 1)
  port = Trim(f.ReadLine)
  f.Close
End If
Set sh = CreateObject("WScript.Shell")
sh.CurrentDirectory = dir
sh.Environment("PROCESS").Item("EBIZ_PORTABLE") = "1"
sh.Environment("PROCESS").Item("EBIZ_DEFAULT_PIN") = "0000"
sh.Run """" & dir & "\EbizOp.Web.exe"" --urls http://0.0.0.0:" & port, 0, False
