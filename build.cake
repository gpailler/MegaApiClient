#addin nuget:?package=Cake.Git
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=xunit.runner.console"
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
var version = AppVeyor.IsRunningOnAppVeyor ? new Version(AppVeyor.Environment.Build.Version).ToString(3) : "1.0.0";

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
   NuGetRestore(solution);
});


Task("Generate-Versionning")
    .Does(() =>
{
    generatedVersion = version + "." + revision;
    Information("Generated version '{0}'", generatedVersion);

    var branch = (AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Repository.Branch : GitBranchCurrent(".").FriendlyName).Replace('/', '-');
    generatedSuffix = (branch == "master" && revision > 0) ? "" : "-" + branch.Substring(0, Math.Min(10, branch.Length)) + "-" + revision;
    Information("Generated suffix '{0}'", generatedSuffix);
});


Task("Patch-GlobalAssemblyVersions")
    .IsDependentOn("Generate-Versionning")
    .Does(() =>
{
    CreateAssemblyInfo(globalAssemblyInfo, new AssemblyInfoSettings {
        FileVersion = generatedVersion,
        InformationalVersion = version + generatedSuffix,
        Version = generatedVersion
        }
    );
});


Task("Build")
    .IsDependentOn("Restore-Packages")
    .IsDependentOn("Patch-GlobalAssemblyVersions")
    .Does(() =>
{
   MSBuild(solution, new MSBuildSettings {
      Configuration = configuration
    });
});


Task("Test")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() =>
{
    OpenCover(tool => {
        tool.XUnit2("./MegaApiClient.Tests/bin/*/MegaApiClient.Tests.dll",
                    new XUnit2Settings {
                        ShadowCopy = false,
                        Parallelism = ParallelismOption.None,
                        ArgumentCustomization = args => args.Append("-appveyor")
                    });
        },
        coverage,
        new OpenCoverSettings { ReturnTargetCodeOffset = 0 }
            .WithFilter("+[*]CG.Web.MegaApiClient*")
            .WithFilter("-[MegaApiClient.Tests]*"));

    if (AppVeyor.IsRunningOnAppVeyor && AppVeyor.Environment.PullRequest != null)
    {
        CoverallsIo(coverage, new CoverallsIoSettings()
        {
            RepoToken = EnvironmentVariable("COVERALLS_REPO_TOKEN")
        });
    }
});


Task("Pack")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() =>
{
    NuGetPack(nuspec, new  NuGetPackSettings {
        Version = version + generatedSuffix,
        Symbols = (AppVeyor.IsRunningOnAppVeyor && AppVeyor.Environment.Repository.Branch == "master" && revision > 0) == false,
        OutputDirectory = artifactsDirectory
    });

});


Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");


RunTarget(target);
