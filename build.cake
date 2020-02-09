#addin nuget:?package=Cake.Git
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=xunit.runner.console"
#tool nuget:?package=Codecov
#addin nuget:?package=Cake.Codecov
#addin nuget:?package=Cake.DocFx

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
var generatedSemVersion = "";

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

    generatedSemVersion = isRCBuild
        ? version
        : string.Join("-", version, branch.Substring(0, Math.Min(10, branch.Length)), revision);
    Information("Generated semantic version '{0}'", generatedSemVersion);
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
            InformationalVersion = generatedSemVersion,
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
            Configuration = "Release",
            ArgumentCustomization = args => args.Append("/p:PackageVersion={0}", generatedSemVersion)
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
                args.Append("/p:PackageVersion={0}", generatedSemVersion);

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
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Patch-GlobalAssemblyVersions")
    .Does(() =>
{
    var testConfiguration = "Debug";

    DotNetCoreBuild(
        solution,
        new DotNetCoreBuildSettings
        {
            Configuration = testConfiguration,
            ArgumentCustomization = args => args.Append("/p:PackageVersion={0}", generatedSemVersion)
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
                Register = "Path32"
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
