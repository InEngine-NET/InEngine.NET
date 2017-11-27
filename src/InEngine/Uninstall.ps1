(Get-WdmiObject win32_service -Filter "name='InEngine.NET'").delete()
