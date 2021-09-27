# Install dotnet-format if needed
# dotnet tool install -g dotnet-format --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json

dotnet format --fix-whitespace --fix-style info MegaApiClient.sln --exclude MegaApiClient\Cryptography\BigInteger.cs MegaApiClient\Cryptography\Crc32.cs MegaApiClient\Cryptography\PBKDF2.cs