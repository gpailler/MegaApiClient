#addin nuget:?package=Cake.Git
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=xunit.runner.console"
#tool nuget:?package=Codecov
#addin nuget:?package=Cake.Codecov

var target = Argument("target", "Default");

var artifactsDirectory = Directory("./artifacts");
var solution = File("./MegaApiClient.sln");
var globalAssemblyInfo = File("./GlobalAssemblyInfo.cs");
var coverageResult = File("./artifacts/opencover.xml");
var revision = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number : 0;
var version = AppVeyor.IsRunningOnAppVeyor ? new Version(AppVeyor.Environment.Build.Version.Split('-')[0]).ToString(3) : "1.0.0";
var isRCBuild = AppVeyor.IsRunningOnAppVeyor
    && AppVeyor.Environment.Repository.Branch == "master"
    && revision > 0
    && AppVeyor.Environment.Repository.Tag.IsTag;
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

    generatedSuffix = isRCBuild
        ? ""
        : branch.Substring(0, Math.Min(10, branch.Length)) + "-" + revision;
    Information("Generated suffix '{0}'", generatedSuffix);
});


Task("Patch-GlobalAssemblyVersions")
    .IsDependentOn("Generate-Versionning")
    .Does(() =>
{
    CreateAssemblyInfo(
        globalAssemblyInfo,
        new AssemblyInfoSettings
        {
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
    DotNetCoreBuild(
        solution,
        new DotNetCoreBuildSettings
        {
            Configuration = "Release"
        }
    );
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
                if (isRCBuild == false)
                {
                    args.Append("--include-symbols");
                }

                return args;
            }
        }
    );
});



Task("Test")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Patch-GlobalAssemblyVersions")
    .Does(() =>
{
    var testConfiguration = "Debug";

    DotNetCoreBuild(
        solution,
        new DotNetCoreBuildSettings
        {
            Configuration = testConfiguration
        }
    );

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        OpenCover(tool =>
            {
                tool.XUnit2(string.Concat("./MegaApiClient.Tests/bin/", testConfiguration, "/net46/*.Tests.dll"));
            },
            coverageResult,
            new OpenCoverSettings
            {
                ReturnTargetCodeOffset = 0,
                Register = "user"
            }
            .WithFilter("+[MegaApiClient]*")
            .WithFilter("-[MegaApiClient.*]*")
        );

        Codecov(coverageResult);

        DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = testConfiguration,
                Framework = "netcoreapp1.1"
            }
        );
    }
    else
    {
        DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = testConfiguration
            }
        );
    }
});


Task("Default")
    .IsDependentOn("Pack")
    .IsDependentOn("Test");


RunTarget(target);
