#tool "nuget:?package=GitVersion.CommandLine&version=5.2.4"
#tool "nuget:?package=OpenCover&version=4.7.922"
#tool "nuget:?package=xunit.runner.console&version=2.4.1"
#tool "nuget:?package=Codecov&version=1.10.0"
#addin "nuget:?package=Cake.Codecov&version=0.8.0"
#addin "nuget:?package=Cake.DocFx&version=0.13.1"
#addin "Cake.Incubator&version=5.1.0"

var target = Argument("target", "Default");

var artifactsDirectory = Directory("./artifacts");
var solution = File("./MegaApiClient.sln");
var globalAssemblyInfo = File("./GlobalAssemblyInfo.cs");
var coverageResult = File("./artifacts/opencover.xml");
GitVersion gitVersion = null;
var isReleaseBuild = false;

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
    gitVersion = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true,
        UpdateAssemblyInfoFilePath = globalAssemblyInfo
    });

    Information("GitVersion details:\n{0}", gitVersion.Dump());

    isReleaseBuild = AppVeyor.IsRunningOnAppVeyor
        && (AppVeyor.Environment.Repository.Branch == "master" || AppVeyor.Environment.Repository.Tag.IsTag);

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        var buildVersion = gitVersion.SemVer + ".ci." + AppVeyor.Environment.Build.Number;
        Information("Using build version: {0}", buildVersion);
        AppVeyor.UpdateBuildVersion(buildVersion);
    }
});


Task("Build")
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Generate-Versionning")
    .Does(() =>
{
    DotNetCoreBuild(
        solution,
        new DotNetCoreBuildSettings
        {
            Configuration = "Release",
            ArgumentCustomization = args => args.Append("/p:PackageVersion={0}", gitVersion.NuGetVersion)
        }
    );
});


Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCorePack(
        "./MegaApiClient/MegaApiClient.csproj",
        new DotNetCorePackSettings
        {
            OutputDirectory = artifactsDirectory,
            ArgumentCustomization = args =>
            {
                args.Append("/p:PackageVersion={0}", gitVersion.NuGetVersion);

                if (!isReleaseBuild)
                {
                    args.Append("--include-symbols");
                }

                return args;
            }
        }
    );
});



Task("Test")
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Generate-Versionning")
    .Does(() =>
{
    var testConfiguration = "Debug";

    DotNetCoreBuild(
        solution,
        new DotNetCoreBuildSettings
        {
            Configuration = testConfiguration,
            ArgumentCustomization = args => args.Append("/p:PackageVersion={0}", gitVersion.NuGetVersion)
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
                Register = "appveyor"
            }
            .WithFilter("+[MegaApiClient]CG.Web.MegaApiClient*")
            .WithFilter("-[MegaApiClient.Tests]*")
        );

        Codecov(coverageResult);

        /*DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = testConfiguration,
                Framework = "netcoreapp2.1",
                NoBuild = true
            }
        );*/
    }
    else
    {
        DotNetCoreTest(
            "./MegaApiClient.Tests/MegaApiClient.Tests.csproj",
            new DotNetCoreTestSettings
            {
                Configuration = testConfiguration,
                NoBuild = true
            }
        );
    }
});



Task("Doc")
    .Does(() =>
{
    DocFxMetadata("./docs/docfx.json");
    DocFxBuild("./docs/docfx.json");

    var htmldoc_root = "./docs/_site";
    var files = GetFiles(htmldoc_root + "/**/*");
    Zip(htmldoc_root, "./artifacts/docfx.zip", files);
});



Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Pack")
    .IsDependentOn("Test")
    .IsDependentOn("Doc");


RunTarget(target);
