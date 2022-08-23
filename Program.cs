using System.Text.RegularExpressions;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

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

        packageId = packageMatch.Success ? packageMatch.Groups[1].Value : projectFile.Name.Replace(".csproj",string.Empty);
        Debug($"Possible package ID: {projectFile.Name.Replace(".csproj",string.Empty)}");
    }
    
    Debug($"Package ID: {packageId}");
    
    var versionMatch = versionPattern.Match(file);
    Version currentProjectVersion;

    if (versionMatch.Success)
    {
        currentProjectVersion = Version.Parse(versionMatch.Groups[1].Value);
        Debug($"Found current version: {currentProjectVersion}");
    }
    else
    {
        Console.WriteLine("::warning file=Program.cs,title=Project version not found::We couldn't identify the version of the project, defaulting to 1.0.0");
        currentProjectVersion = Version.Parse("1.0.0");
    }
    
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

    var nuGetVersions = remotePackageVersions as NuGetVersion[] ?? remotePackageVersions.ToArray();
    if (nuGetVersions.Any(t => t.Version == currentProjectVersion))
        throw new ApplicationException($"The version {currentProjectVersion} is already in use!");

    foreach (var version in nuGetVersions)
    {
        Debug($"Found version: {version.Version}");
    }
    
    Debug("No matching version found. Success!");
}
catch (Exception e)
{
    Console.WriteLine($"::error file=Program.cs, title={e.Source}::{e.Message}");
}

void Debug(string text)
{
    Console.WriteLine($"::debug::{text}");
}

