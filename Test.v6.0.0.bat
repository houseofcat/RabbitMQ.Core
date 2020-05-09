set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test --no-build --logger "console;verbosity=detailed" ./RabbitMQ.Core.Client.6.0.0.sln
