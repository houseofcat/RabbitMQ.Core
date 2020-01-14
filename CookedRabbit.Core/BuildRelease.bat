// https://github.com/dotnet/runtime/blob/master/src/libraries/pkg/Microsoft.NETCore.Platforms/runtime.json

dotnet build --configuration Release --runtime win10-x64

dotnet build --configuration Release --runtime linux-x64

dotnet build --configuration Release --runtime centos-x64

dotnet build --configuration Release --runtime osx.10.15

dotnet pack

dotnet publish