#tool GitVersion.CommandLine&version=5.0.1
#tool "nuget:?package=coverlet.msbuild&version=2.6.3"

var target = Argument("Target", "Build");
var configuration = Argument("Configuration", "Release");
var runtime = Argument("runtime", (string)null);
var packageSource = Argument("packageSource", (string)null);

var packageProject = File(Argument("PackageProject", "./ClassLibrary/ClassLibrary.csproj"));
var packageApiKey = EnvironmentVariable("PACKAGE_API_KEY");

var packageDirectory = MakeAbsolute(Directory("./pack"));
var publishDirectory = MakeAbsolute(Directory("./publish"));

var gitVersion = GitVersion();

Information($"Target: {target}");
Information($"Configuration: {configuration}");
Information($"PublishDirectory: {publishDirectory}");
Information($"PackageDirectory: {packageDirectory}");
Information($"PackageProject: {packageProject}");
Information($"PackageSource: {packageSource}");
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

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => Pack());

Task("Push")
    .IsDependentOn("Pack")
    .Does(() => Push());

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
    CleanDirectory(packageDirectory);
}

private void Restore()
{
    DotNetCoreRestore();
}
 
private void Build()
{
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
            OutputDirectory = publishDirectory.ToString(),
            Runtime = runtime,
            MSBuildSettings = GetBuildSettings()
        });
}

private void Pack()
{
    var packSettings = new DotNetCorePackSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .SetMaxCpuCount(0)
            .SetConfiguration(configuration)
            .WithProperty("PackageOutputPath", packageDirectory.ToString())
            .WithProperty("RepositoryCommit", gitVersion.Sha)
            .WithProperty("Version", gitVersion.NuGetVersionV2)
            .WithProperty("PackageVersion", gitVersion.NuGetVersionV2)
            .WithProperty("AssemblyVersion", gitVersion.AssemblySemVer)
            .WithProperty("FileVersion", gitVersion.MajorMinorPatch)
            .WithProperty("InformationalVersion", gitVersion.InformationalVersion)
    };

    DotNetCorePack(packageProject, packSettings);
}

private void Push()
{
    if (string.IsNullOrWhiteSpace(packageApiKey))
        throw new ArgumentNullException(nameof(packageApiKey), "API Key is missing");

    if (string.IsNullOrWhiteSpace(packageSource))
        throw new ArgumentNullException(nameof(packageSource), "Package source is missing");

    var package = GetFiles(MakeAbsolute(packageDirectory).ToString() + "/*.nupkg").FirstOrDefault();
    if (package == null)
        throw new Exception("Package not found");

    var pushSettings = new NuGetPushSettings
    {
        ApiKey = packageApiKey,
        Source = packageSource
    };

    NuGetPush(package, pushSettings);
}

private DotNetCoreMSBuildSettings GetBuildSettings()
{
    return new DotNetCoreMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", gitVersion.NuGetVersionV2)
        .WithProperty("AssemblyVersion", gitVersion.AssemblySemVer)
        .WithProperty("FileVersion", gitVersion.MajorMinorPatch)
        .WithProperty("InformationalVersion", gitVersion.InformationalVersion);
}

RunTarget(target);