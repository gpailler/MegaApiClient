#addin nuget:?package=Cake.Git
#tool "nuget:?package=OpenCover"
#tool coveralls.io
#addin Cake.Coveralls
#tool "nuget:?package=xunit.runner.console"

var configuration = Argument("configuration", "Release");
var target = Argument("target", "Default");

var artifactsDirectory = Directory("./artifacts");
var reportsDirectory = Directory("./reports");
var solution = File("./MegaApiClient.sln");
var globalAssemblyInfo = File("./GlobalAssemblyInfo.cs");
var coverageResult = File("./reports/opencover.xml");
var revision = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number : 0;
var version = AppVeyor.IsRunningOnAppVeyor ? new Version(AppVeyor.Environment.Build.Version.Split('-')[0]).ToString(3) : "1.0.0";

var generatedVersion = "";
var generatedSuffix = "";


Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDirectory);
    CleanDirectory(reportsDirectory);
});


Task("Restore-Packages")
    .Does(() =>
{
   DotNetCoreRestore(solution);
});


Task("Generate-Versionning")
    .Does(() =>
{
    generatedVersion = version + "." + revision;
    Information("Generated version '{0}'", generatedVersion);

    var branch = AppVeyor.IsRunningOnAppVeyor
        ? AppVeyor.Environment.PullRequest.IsPullRequest
            ? AppVeyor.Environment.Build.Version.Split('-')[1]
            : AppVeyor.Environment.Repository.Branch
        : GitBranchCurrent(".").FriendlyName;
    branch = branch.Replace('/', '-');

    generatedSuffix = (branch == "master" && revision > 0) ? "" : branch.Substring(0, Math.Min(10, branch.Length)) + "-" + revision;
    Information("Generated suffix '{0}'", generatedSuffix);
});


Task("Patch-GlobalAssemblyVersions")
    .IsDependentOn("Generate-Versionning")
    .Does(() =>
{
    CreateAssemblyInfo(globalAssemblyInfo, new AssemblyInfoSettings {
        FileVersion = generatedVersion,
        InformationalVersion = version + "-" + generatedSuffix,
        Version = generatedVersion
        }
    );
});


Task("Build")
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Patch-GlobalAssemblyVersions")
    .Does(() =>
{
   DotNetCoreBuild(solution, new DotNetCoreBuildSettings {
      Configuration = configuration
    });
});


Task("Test")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() =>
{
    if (IsRunningOnWindows())
    {
        OpenCover(tool => {
            tool.XUnit2("./MegaApiClient.Tests/bin/Release/net46/*.Tests.dll",
            new XUnit2Settings {
                HtmlReport = true,
                XmlReport = true,
                OutputDirectory = reportsDirectory
            });
        },
        coverageResult,
        new OpenCoverSettings { ReturnTargetCodeOffset = 0 }
        .WithFilter("+[*]CG.Web.MegaApiClient*")
        .WithFilter("-[MegaApiClient.Tests]*"));

        DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = configuration,
                Framework = "netcoreapp1.1"
            });
    }
    else
    {
        DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = configuration
            });
    }
});


Task("Pack")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCorePack(
        "./MegaApiClient/MegaApiClient.csproj",
        new DotNetCorePackSettings
        {
            VersionSuffix = generatedSuffix,
            OutputDirectory = artifactsDirectory,
            ArgumentCustomization = args =>
            {
                if ((AppVeyor.IsRunningOnAppVeyor && AppVeyor.Environment.Repository.Branch == "master" && revision > 0) == false)
                {
                    args.Append("--include-symbols");
                }

                return args;
            }
        });
});


Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");


RunTarget(target);
