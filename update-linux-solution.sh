#!/bin/bash

# update-linux-solution.sh - Regenerate the Hastlayer.SDK.Linux.sln file the main Hastlayer.SDK.sln.

cp Hastlayer.SDK.sln Hastlayer.SDK.Linux.sln
dotnet sln Hastlayer.SDK.Linux.sln remove Samples/Hast.Samples.Kpz/Hast.Samples.Kpz.csproj
dotnet sln Hastlayer.SDK.Linux.sln remove Hast.Core/Hast.Remote.Worker.Daemon/Hast.Remote.Worker.Daemon.csproj
