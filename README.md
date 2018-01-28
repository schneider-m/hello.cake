# Building .NET Core with Cake

A simple example for building .NET Core apps with Cake including targets for

- Clean
- Version (GitVersion, generates AssemblyInfo.cs)
- Restore
- Build
- Test (NUnit, *.Tests.csproj)
- Publish (framework-dependent or self-contained)


## Usage examples

Build:

    ./build.ps1 -target="Build"

Run tests:

    ./build.ps1 -target="Test"

Publish a self-contained app targeting win10-x64:
    
    ./build.ps1 -target="Publish" -runtime="win10-x64"


## FAQ

**Build error CS0579: Duplicate 'System.Reflection.AssemblyCompanyAttribute'**

Disable automatic generation of assembly information for every project via

    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>

**Test execution fails with: No test is available in ...**

All test projects must target `netcoreapp` not `netstandard`.
