#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("Target", "Build");  
var configuration = Argument("Configuration", "Release");
var publishDirectory = Argument("PublishDirectory", "publish");
var runtime = Argument("runtime", (string)null);

Information($"Target: {target}");
Information($"Configuration: {configuration}");
Information($"PublishDirectory: {publishDirectory}");
Information($"Runtime: {runtime}");

Func<IFileSystemInfo, bool> excludeGitFolder = fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains(".git");

Task("Clean")  
    .Does(() =>
    {
		var projects = GetFiles("./**/*.csproj", excludeGitFolder);
        foreach(var project in projects)
        {
			Information("Cleaning project " + project);
			DotNetCoreClean(project.ToString(), new DotNetCoreCleanSettings
			{
				Configuration = configuration,
			});	
		}
		
		CleanDirectory(publishDirectory);
    });

Task("Version")  
    .Does(() =>
    {
        var gitVersion = GitVersion();		
		var solutionInfo = "./SolutionInfo.cs";
		CreateAssemblyInfo(solutionInfo, new AssemblyInfoSettings
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

		var projects = GetFiles("./**/*.csproj", excludeGitFolder);
        foreach(var project in projects)
        {
			var assemblyInfo = System.IO.Path.Combine(project.GetDirectory().FullPath, "AssemblyInfo.cs");
			CopyFile(solutionInfo, assemblyInfo);			
		}
    });

Task("Restore")  
    .Does(() =>
    {
        DotNetCoreRestore();
    });

Task("Build")
    .Does(() =>
    {
		DotNetCoreBuild(".",
            new DotNetCoreBuildSettings()
            {				
                Configuration = configuration,
				NoRestore = true
            });
    })
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("Version");    

Task("Test")  
    .Does(() =>
    {
        var projects = GetFiles("./**/*Tests.csproj", excludeGitFolder);
        foreach(var project in projects)
        {
            Information("Testing project " + project);
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoRestore = true,
					NoBuild = true
                });
        }
    })
	.IsDependentOn("Build");;

	Task("Publish")
    .Does(() =>
    {
		var ds = new DotNetCorePublishSettings();
		DotNetCorePublish(".",
            new DotNetCorePublishSettings()
            {				
                Configuration = configuration,
				OutputDirectory = publishDirectory,
				Runtime = runtime
            });
    })
	.IsDependentOn("Test");

RunTarget(target);  