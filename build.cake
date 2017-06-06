using System.Xml.Linq;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

const string libraryProject = "./src/Unity.AutoFactory/Unity.AutoFactory.csproj";
const string testProject = "./tests/Unity.AutoFactory.Tests/Unity.AutoFactory.Tests.csproj";

string versionSuffix = GetVersionSuffix();


string GetVersionSuffix()
{
    Information("Determining version suffix...");

    var version = GetVersionFromProject();

    var parts = new List<string>();
    if (!string.IsNullOrEmpty(version.Suffix))
    {
        parts.Add(version.Suffix);
    }

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        if (AppVeyor.Environment.Repository.Tag.IsTag)
        {
            string expectedTagName = string.IsNullOrEmpty(version.Suffix)
                ? version.Prefix
                : $"{version.Prefix}-{version.Suffix}";
            
            if (AppVeyor.Environment.Repository.Tag.Name == expectedTagName)
            {
                Information("The tag name and the version information in the project file match; building a release build");
            }
            else
            {
                Warning("The tag name and the version information in the project file don't match; adding adhoc version suffix");
                parts.Add($"build{AppVeyor.Environment.Build.Number:D4}");
            }
        }
        else
        {
            Information("This is an AppVeyor build, but not a tag; adding adhoc version suffix");
            parts.Add($"build{AppVeyor.Environment.Build.Number:D4}");
        }
    }
    else
    {
        Information("Non-CI build; adding adhoc version suffix");
        parts.Add("adhoc");
    }

    string versionSuffix = string.Join("-", parts);
    Information($"Version suffix: {versionSuffix}");
    return versionSuffix;
}

class VersionInfo
{
    public string Prefix { get; set; }
    public string Suffix { get; set; }
}

VersionInfo GetVersionFromProject()
{
    var doc = XDocument.Load(MakeAbsolute(File(libraryProject)).FullPath);
    string prefix = doc.Root.Elements("PropertyGroup").Elements("VersionPrefix").FirstOrDefault()?.Value ?? "0.0.0";
    string suffix = doc.Root.Elements("PropertyGroup").Elements("VersionSuffix").FirstOrDefault()?.Value ?? "";
    return new VersionInfo { Prefix = prefix, Suffix = suffix };
}

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
        Configuration = configuration,
        VersionSuffix = versionSuffix
    });
});

Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest(testProject, new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    });
});

Task("Pack")
    .IsDependentOn("RunTests")
    .Does(() =>
{
    DotNetCorePack(
        libraryProject,
        new DotNetCorePackSettings
        {
            Configuration = configuration,
            VersionSuffix = versionSuffix,
            NoBuild = true
        });
});

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);
