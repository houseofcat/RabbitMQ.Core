dotnet build -c Release --runtime win-x64
dotnet build -c Release --runtime win10-x64
dotnet build -c Release --runtime linux-x64
dotnet build -c Release --runtime linux-musl-x64
dotnet build -c Release --runtime osx-x64
dotnet build -c Release --runtime centos-x64
dotnet build -c Release --runtime debian-x64

dotnet publish -f netstandard2.0 -c Release -r win-x64 --no-build -o '.\runtimes\win-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r win10-x64 --no-build -o '.\runtimes\win10-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r linux-x64 --no-build -o '.\runtimes\linux-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r linux-musl-x64 --no-build -o '.\runtimes\linux-musl-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r osx-x64 --no-build -o '.\runtimes\osx-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r centos-x64 --no-build -o '.\runtimes\centos-x64\lib\netstandard2.0'
dotnet publish -f netstandard2.0 -c Release -r debian-x64 --no-build -o '.\runtimes\debian-x64\lib\netstandard2.0'

dotnet publish -f netstandard2.1 -c Release -r win-x64 --no-build -o '.\runtimes\win-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r win10-x64 --no-build -o '.\runtimes\win10-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r linux-x64 --no-build -o '.\runtimes\linux-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r linux-musl-x64 --no-build -o '.\runtimes\linux-musl-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r osx-x64 --no-build -o '.\runtimes\osx-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r centos-x64 --no-build -o '.\runtimes\centos-x64\lib\netstandard2.1'
dotnet publish -f netstandard2.1 -c Release -r debian-x64 --no-build -o '.\runtimes\debian-x64\lib\netstandard2.1'

dotnet publish -f netcoreapp2.2 -c Release -r win-x64 --no-build -o '.\runtimes\win-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r win10-x64 --no-build -o '.\runtimes\win10-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r linux-x64 --no-build -o '.\runtimes\linux-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r linux-musl-x64 --no-build -o '.\runtimes\linux-musl-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r osx-x64 --no-build -o '.\runtimes\osx-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r centos-x64 --no-build -o '.\runtimes\centos-x64\lib\netcoreapp2.2'
dotnet publish -f netcoreapp2.2 -c Release -r debian-x64 --no-build -o '.\runtimes\debian-x64\lib\netcoreapp2.2'

dotnet publish -f netcoreapp3.0 -c Release -r win-x64 --no-build -o '.\runtimes\win-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r win10-x64 --no-build -o '.\runtimes\win10-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r linux-x64 --no-build -o '.\runtimes\linux-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r linux-musl-x64 --no-build -o '.\runtimes\linux-musl-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r osx-x64 --no-build -o '.\runtimes\osx-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r centos-x64 --no-build -o '.\runtimes\centos-x64\lib\netcoreapp3.0'
dotnet publish -f netcoreapp3.0 -c Release -r debian-x64 --no-build -o '.\runtimes\debian-x64\lib\netcoreapp3.0'

dotnet publish -f netcoreapp3.1 -c Release -r win-x64 --no-build -o '.\runtimes\win-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r win10-x64 --no-build -o '.\runtimes\win10-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r linux-x64 --no-build -o '.\runtimes\linux-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r linux-musl-x64 --no-build -o '.\runtimes\linux-musl-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r osx-x64 --no-build -o '.\runtimes\osx-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r centos-x64 --no-build -o '.\runtimes\centos-x64\lib\netcoreapp3.1'
dotnet publish -f netcoreapp3.1 -c Release -r debian-x64 --no-build -o '.\runtimes\debian-x64\lib\netcoreapp3.1'