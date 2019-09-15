# Building .NET Core with Cake

A simple example for building .NET Core apps with Cake including targets for

- Clean
- Restore
- Version (GitVersion, generates AssemblyInfo.cs)
- Build
- Test (NUnit, *.Tests.csproj)
- Publish (framework-dependent or self-contained)


## Usage examples

Build:

    ./build.ps1 --target="Build"

Run tests:

    ./build.ps1 --target="Test"
    
Create a nuget package:

    ./build.ps1 --target="Pack" --packageProject=".\ClassLibrary\ClassLibrary.csproj"

Publish a self-contained app targeting win10-x64:
    
    ./build.ps1 -target="Publish" -runtime="win10-x64"