FROM ubuntu:24.04

# .NET Core

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates gnupg wget \
  && wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb \
  && dpkg -i /tmp/packages-microsoft-prod.deb \
  && rm -f /tmp/packages-microsoft-prod.deb \
  && apt-get update \
  && apt-get install -y --no-install-recommends dotnet-sdk-10.0 \
  && rm -rf /var/lib/apt/lists/*

# MoonSharp

WORKDIR /build

COPY ci.sh ci.sh
COPY src/MoonSharp.Interpreter src/MoonSharp.Interpreter/
COPY src/MoonSharp.Interpreter.Tests src/MoonSharp.Interpreter.Tests/
COPY src/MoonSharp.RemoteDebugger src/MoonSharp.RemoteDebugger/
COPY src/MoonSharp.VsCodeDebugger src/MoonSharp.VsCodeDebugger/
COPY src/TestRunners/DotNetCoreTestRunner src/TestRunners/DotNetCoreTestRunner/
COPY src/moonsharp_ci.sln src/moonsharp_ci.sln

ENTRYPOINT ["sh", "/build/ci.sh"]
