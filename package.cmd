rm *.nupkg
%userprofile%\bin\nuget.exe pack IntegrationEngine.Model\IntegrationEngine.Model.csproj -Prop Configuration=Release
%userprofile%\bin\nuget.exe pack IntegrationEngine.Core\IntegrationEngine.Core.csproj -Prop Configuration=Release
%userprofile%\bin\nuget.exe pack IntegrationEngine\IntegrationEngine.csproj -Prop Configuration=Release
