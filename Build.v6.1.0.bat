@ECHO OFF
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet restore .\RabbitMQ.Core.Client.6.1.0.sln
dotnet run -p .\v6.1.0\Apigen\Apigen.csproj --apiName:AMQP_0_9_1 .\v6.1.0\Docs\specs\amqp0-9-1.stripped.xml .\v6.1.0\RabbitMQ.Client\amqp.cs --framework netcoreapp3.1
dotnet build .\RabbitMQ.Core.Client.6.1.0.sln
