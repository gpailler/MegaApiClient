#addin nuget:?package=Cake.Git
#tool "nuget:?package=OpenCover"
#tool coveralls.io
#addin Cake.Coveralls

var configuration = Argument("configuration", "Release");
var target = Argument("target", "Default");

var artifactsDirectory = Directory("./artifacts");
var solution = File("./MegaApiClient.sln");
var nuspec = File("./MegaApiClient.nuspec");
var globalAssemblyInfo = File("./GlobalAssemblyInfo.cs");
var coverage = File("./artifacts/opencoverCoverage.xml");
var revision = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number : 0;
var version = AppVeyor.IsRunningOnAppVeyor ? new Version(AppVeyor.Environment.Build.Version.Split('-')[0]).ToString(3) : "1.0.0";

var generatedVersion = "";
var generatedSuffix = "";


Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDirectory);
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
    DotNetCoreTest(
        "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
        new DotNetCoreTestSettings
        {
            Configuration = configuration
        });
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
