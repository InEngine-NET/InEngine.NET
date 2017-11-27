$pathToExecutable = Join-Path (Resolve-Path .\).Path "inengine.exe -s"
New-Service -Name "InEngine.NET" -BinaryPathName $pathToExecutable -DependsOn NetLogon -DisplayName "InEngine.NET" -StartupType Manual -Description "Runs InEngine.NET's scheduled jobs."
