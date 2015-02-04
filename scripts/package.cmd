rm *.nupkg
.nuget\nuget.exe pack IntegrationEngine.Model\IntegrationEngine.Model.csproj -Prop Configuration=Release -Prop Platform=AnyCPU
.nuget\nuget.exe pack IntegrationEngine.Client\IntegrationEngine.Client.csproj -Prop Configuration=Release -Prop Platform=AnyCPU
.nuget\nuget.exe pack IntegrationEngine.Core\IntegrationEngine.Core.csproj -Prop Configuration=Release -Prop Platform=AnyCPU
.nuget\nuget.exe pack IntegrationEngine\IntegrationEngine.csproj -Prop Configuration=Release -Prop Platform=AnyCPU
