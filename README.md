# Building .NET Core with Cake

A simple example for building .NET Core apps with Cake including targets for

- Clean
- Restore
- Version (GitVersion)
- Build
- Test (NUnit, *.Tests.csproj)
- Publish (framework-dependent or self-contained)
- Pack (NuGet)
- Push (NuGet)


## Usage examples

First you have to install the [Cake .NET Core Global Tool](https://www.nuget.org/packages/Cake.Tool/)

    dotnet tool install --global Cake.Tool --version 0.34.1
    
Then you can invoke the tasks like this:

Build:

    dotnet cake -target="Build"

Run tests:

    dotnet cake -target="Test"

Create a nuget package:

    dotnet cake -target="Pack" -packageProject=".\ClassLibrary\ClassLibrary.csproj"

Push a nuget package to a package source:

    dotnet cake -target="Push" -packageProject=".\ClassLibrary\ClassLibrary.csproj" -packageSource="c:\myLocalPackageSource"

Publish a self-contained app targeting win10-x64:
    
    dotnet cake -target="Publish" -runtime="win10-x64"