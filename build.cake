#tool GitVersion.CommandLine&version=5.0.1
#tool "nuget:?package=coverlet.msbuild&version=2.6.3"

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

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() => Build());

Task("Test")
	.IsDependentOn("Build")
	.Does(() => Test());

Task("Publish")
	.IsDependentOn("Test")
    .Does(() => Publish());

var solutionFile = GetFiles("./*.sln").First();
var solution = new Lazy<SolutionParserResult>(() => ParseSolution(solutionFile));
var solutionProjects = new Lazy<IReadOnlyCollection<SolutionProject>>(() => solution.Value.Projects);
var projectPaths = new Lazy<IEnumerable<FilePath>>(() => solutionProjects.Value
	.Select(x => x.Path)
	.Where(x => x.HasExtension));

private void Clean()
{
    foreach (var project in projectPaths.Value)
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
 
private void Build()
{
    var gitVersion = GitVersion();
    var buildSettings = new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            MSBuildSettings = GetBuildSettings()
        };

	DotNetCoreBuild(".", buildSettings);
}

private void Test()
{
	var testProjects = projectPaths.Value.Where(x => x.FullPath.EndsWith("Tests.csproj"));
    foreach (var project in testProjects)
    {
        Information("Testing project " + project);
        DotNetCoreTest(project.ToString(),
            new DotNetCoreTestSettings()
            {
                Configuration = configuration,
                NoRestore = true,
                NoBuild = true,
				ArgumentCustomization = args=>args.Append("/p:CollectCoverage=true /p:Exclude=[NUnit*]*")
            });
    }
}

private void Publish()
{    
    DotNetCorePublish(".",
        new DotNetCorePublishSettings()
        {
            Configuration = configuration,
            OutputDirectory = publishDirectory,
            Runtime = runtime,
			MSBuildSettings = GetBuildSettings()
        });
}

private DotNetCoreMSBuildSettings GetBuildSettings()
{
    var gitVersion = GitVersion();
    return new DotNetCoreMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", gitVersion.NuGetVersionV2)
        .WithProperty("AssemblyVersion", gitVersion.AssemblySemVer)
        .WithProperty("FileVersion", gitVersion.MajorMinorPatch)
        .WithProperty("InformationalVersion", gitVersion.InformationalVersion);
}

RunTarget(target);