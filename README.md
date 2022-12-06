# blazor-Wasm

dotnet new blazorwasm -o src/Client  
dotnet new classlib -o src/Client.Infrastructure  
dotnet new sln --name WepApp
dotnet new webapi -o src/Host  
dotnet sln add src/Client/Client.csproj  
dotnet sln add src/Client/Client.Infrastructure.csproj  
dotnet sln add src/Host/Host.csproj  
