#!/usr/bin/env sh

set -e

export DOTNET_ROLL_FORWARD=Major

# Build
dotnet build -c Release src/TestRunners/DotNetCoreTestRunner/DotNetCoreTestRunner.csproj

# .NET Core Tests
cd src/TestRunners/DotNetCoreTestRunner/bin/Release/net10.0
dotnet DotNetCoreTestRunner.dll /unit
