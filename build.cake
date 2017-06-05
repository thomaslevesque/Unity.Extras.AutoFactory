var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new[]
    {
        $"./src/Unity.AutoFactory/bin/{configuration}",
        $"./src/Unity.AutoFactory/obj/{configuration}",
        $"./tests/Unity.AutoFactory.Tests/bin/{configuration}",
        $"./tests/Unity.AutoFactory.Tests/obj/{configuration}"
    });
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore();
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreBuild("", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testProjects = new[]
    {
        "./tests/Unity.AutoFactory.Tests/Unity.AutoFactory.Tests.csproj"
    };

    foreach (var project in testProjects)
    {
        DotNetCoreTest(project, new DotNetCoreTestSettings
        {
           Configuration = configuration,
           NoBuild = true
        });
    }
});

Task("Default")
    .IsDependentOn("RunTests");

RunTarget(target);
