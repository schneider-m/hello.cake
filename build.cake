#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("Target", "Build");
var configuration = Argument("Configuration", "Release");
var publishDirectory = Argument("PublishDirectory", "publish");
var runtime = Argument("runtime", (string)null);

Information($"Target: {target}");
Information($"Configuration: {configuration}");
Information($"PublishDirectory: {publishDirectory}");
Information($"Runtime: {runtime}");

Task("Clean")
    .Does(() => Clean());

Task("Restore")
    .Does(() => Restore());

Task("Version")
    .Does(() => Version());

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("Version")
    .Does(() => Build());

Task("Test")
	.IsDependentOn("Build")
	.Does(() => Test());

Task("Publish")
	.IsDependentOn("Test")
    .Does(() => Publish());

private void Clean()
{
    var projects = GetFiles("./**/*.csproj");
    foreach (var project in projects)
    {
        Information("Cleaning project " + project);
        DotNetCoreClean(project.ToString(), new DotNetCoreCleanSettings
        {
            Configuration = configuration,
        });
    }
    CleanDirectory(publishDirectory);
}

private void Restore()
{
    DotNetCoreRestore();
}

private void Version()
{
    var gitVersion = GitVersion();
    var solutionInfo = "./SolutionInfo.cs";
    CreateAssemblyInfo(solutionInfo,
        new AssemblyInfoSettings
        {
            Company = "MyCompany",
            Product = "MyProduct",
            Description = "",
            Copyright = string.Format("Copyright (c) MyCompany"),
            Configuration = configuration,
            ComVisible = false,
            Version = gitVersion.AssemblySemVer,
            FileVersion = gitVersion.AssemblySemFileVer,
            InformationalVersion = gitVersion.InformationalVersion
        });

    var projects = GetFiles("./**/*.csproj");
    foreach (var project in projects)
    {
        var assemblyInfo = System.IO.Path.Combine(project.GetDirectory().FullPath, "AssemblyInfo.cs");
        CopyFile(solutionInfo, assemblyInfo);
    }
}

public void Build()
{
    DotNetCoreBuild(".",
        new DotNetCoreBuildSettings()
        {
            Configuration = configuration,
            NoRestore = true
        });
}

public void Test()
{
    var projects = GetFiles("./**/*Tests.csproj");
    foreach (var project in projects)
    {
        Information("Testing project " + project);
        DotNetCoreTest(project.ToString(),
            new DotNetCoreTestSettings()
            {
                Configuration = configuration,
                NoRestore = true,
                NoBuild = true
            });
    }
}

public void Publish()
{
    DotNetCorePublish(".",
        new DotNetCorePublishSettings()
        {
            Configuration = configuration,
            OutputDirectory = publishDirectory,
            Runtime = runtime
        });
}

RunTarget(target);