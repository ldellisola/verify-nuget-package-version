using System.Text.RegularExpressions;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;


try
{
    var projectFile = new FileInfo(
        Environment.GetEnvironmentVariable("INPUT_PROJECT_FILE_PATH")
        ?? throw new ArgumentNullException("INPUT_PROJECT_FILE_PATH")
    );

    Debug($"Project file: {projectFile.FullName}");
    Debug($"    Exists? {projectFile.Exists}");

    var file = await File.ReadAllTextAsync(projectFile.FullName);

    var versionPattern = new Regex(
        Environment.GetEnvironmentVariable("INPUT_VERSION_REGEX")
        ?? throw new ArgumentNullException("INPUT_VERSION_REGEX"),
        RegexOptions.Multiline
    );

    Debug($"Version Pattern: {versionPattern}");

    var packageId = Environment.GetEnvironmentVariable("INPUT_PACKAGE_ID");

    if (string.IsNullOrWhiteSpace(packageId))
    {
        Debug("PackageID not defined as argument");

        var packageRegex = new Regex(
            Environment.GetEnvironmentVariable("INPUT_PACKAGE_REGEX")
            ?? throw new ArgumentNullException("INPUT_PACKAGE_REGEX"),
            RegexOptions.Multiline
        );

        Debug($"Package ID Pattern: {packageRegex}");

        var packageMatch = packageRegex.Match(file);

        packageId = packageMatch.Success
            ? packageMatch.Groups[1].Value
            : projectFile.Name.Replace(".csproj", string.Empty);
        Debug($"Possible package ID: {projectFile.Name.Replace(".csproj", string.Empty)}");
    }

    Debug($"Package ID: {packageId}");

    var versionMatch = versionPattern.Match(file);
    var currentProjectVersion = versionMatch.Success ? versionMatch.Groups[1].Value : "1.0.0";

    if (versionMatch.Success is false)
        Console.WriteLine(
            "::warning file=Program.cs,title=Project version not found::We couldn't identify the version of the project, defaulting to 1.0.0");

    Debug("Getting data from NuGet...");

    var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
    var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

    Debug("Looking for specific package ID...");

    var remotePackageVersions = await resource.GetAllVersionsAsync(
        packageId,
        new NullSourceCacheContext(),
        NullLogger.Instance,
        default
    );

    ArgumentNullException.ThrowIfNull(remotePackageVersions);

    if (remotePackageVersions.Any(t =>
            t.ToNormalizedString().Equals(currentProjectVersion, StringComparison.OrdinalIgnoreCase)))
        throw new ApplicationException($"The version {currentProjectVersion} is already in use!");

    Debug("No matching version found. Success!");
}
catch (Exception e)
{
    Console.WriteLine($"::error file=Program.cs, title={e.Source}::{e.Message}");
    Environment.Exit(-1);
}

void Debug(string text)
{
    Console.WriteLine($"::debug::{text}");
}