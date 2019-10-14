#tool GitVersion.CommandLine&version=5.0.1
#tool "nuget:?package=coverlet.msbuild&version=2.6.3"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var runtime = Argument("runtime", (string)null);
var packageSource = Argument("packageSource", (string)null);
var packageApiKey = Argument("packageApiKey", (string)null);
var packageProject = Argument("packageProject", (string)null);

var packageDirectory = MakeAbsolute(Directory("./pack")).ToString();
var publishDirectory = MakeAbsolute(Directory("./publish")).ToString();

var gitVersion = GitVersion();

Information($"Target: {target}");
Information($"Configuration: {configuration}");
Information($"PublishDirectory: {publishDirectory}");
Information($"PackageDirectory: {packageDirectory}");
Information($"PackageProject: {packageProject}");
Information($"PackageSource: {packageSource}");
Information($"Runtime: {runtime}");

var projectPaths = GetProjectPath();
var msBuildSettings = GetBuildSettings(configuration, gitVersion);

private IEnumerable<string> GetProjectPath()
{
    var solutionFile = GetFiles("./*.sln").First();
    var solution = ParseSolution(solutionFile);
    var solutionProjects = solution.Projects;
    return solutionProjects
        .Where(x => x.Path.HasExtension)
        .Select(x => x.Path.ToString());
}

private static DotNetCoreMSBuildSettings GetBuildSettings(string configuration, GitVersion gitVersion)
{
    return new DotNetCoreMSBuildSettings()
        .SetConfiguration(configuration)
        .WithProperty("Version", gitVersion.NuGetVersionV2)
        .WithProperty("AssemblyVersion", gitVersion.AssemblySemVer)
        .WithProperty("FileVersion", gitVersion.MajorMinorPatch)
        .WithProperty("InformationalVersion", gitVersion.InformationalVersion);
}

Task("Clean")
    .Does(() => Clean(projectPaths, publishDirectory, packageDirectory));

Task("Restore")
    .Does(() => Restore());

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => Build(configuration, msBuildSettings));

Task("Test")
    .IsDependentOn("Build")
    .Does(() => Test(projectPaths));

Task("Publish")
    .IsDependentOn("Test")
    .Does(() => Publish(configuration, runtime, publishDirectory, msBuildSettings));

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => Pack());

Task("Push")
    .IsDependentOn("Pack")
    .Does(() => Push());

private void Clean(IEnumerable<string> projectPaths, params DirectoryPath[] additionalDirectories)
{
    foreach (var project in projectPaths)
    {
        Information($"Cleaning project: {project}");
        DotNetCoreClean(project, new DotNetCoreCleanSettings
        {
            Configuration = configuration,
        });
    }

    foreach (var additionalDirectory in additionalDirectories)
    {
        Information($"Cleaning directory: {additionalDirectory}");
        CleanDirectory(additionalDirectory);
    }
}

private void Restore()
{
    DotNetCoreRestore();
}
 
private void Build(string configuration, DotNetCoreMSBuildSettings msBuildSettings)
{
    var buildSettings = new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            MSBuildSettings = msBuildSettings
        };
    DotNetCoreBuild(".", buildSettings);
}

private void Test(IEnumerable<string> projectPaths)
{
    var testProjects = projectPaths.Where(x => x.EndsWith("Tests.csproj"));
    foreach (var project in testProjects)
    {
        Information($"Testing project: {project}");
        DotNetCoreTest(project,
            new DotNetCoreTestSettings()
            {
                Configuration = configuration,
                NoRestore = true,
                NoBuild = true,
                ArgumentCustomization = args=>args.Append("/p:CollectCoverage=true /p:Exclude=[NUnit*]*")
            });
    }
}

private void Publish(string configuration, string runtime, DirectoryPath publishDirectory, DotNetCoreMSBuildSettings msBuildSettings)
{    
    DotNetCorePublish(".",
        new DotNetCorePublishSettings()
        {
            Configuration = configuration,
            OutputDirectory = publishDirectory,
            Runtime = runtime,
            MSBuildSettings = msBuildSettings
        });
}

private void Pack()
{
    if (string.IsNullOrWhiteSpace(packageProject))
        throw new ArgumentNullException(nameof(packageProject), "No project to package specified");

    var packSettings = new DotNetCorePackSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .SetMaxCpuCount(0)
            .SetConfiguration(configuration)
            .WithProperty("PackageOutputPath", MakeAbsolute(File(packageDirectory)).ToString())
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
    if (string.IsNullOrWhiteSpace(packageSource))
        throw new ArgumentNullException(nameof(packageSource), "Package source is missing");

    var package = GetFiles(packageDirectory + "/*.nupkg").FirstOrDefault();
    if (package == null)
        throw new Exception("Package not found");

    var pushSettings = new NuGetPushSettings
    {
        ApiKey = packageApiKey,
        Source = packageSource
    };

    NuGetPush(package, pushSettings);
}

RunTarget(target);