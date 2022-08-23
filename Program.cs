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
    
    var file = await File.ReadAllTextAsync(projectFile.FullName);

    var versionPattern = new Regex(
        Environment.GetEnvironmentVariable("INPUT_VERSION_REGEX")
        ?? throw new ArgumentNullException("INPUT_VERSION_REGEX"),
        RegexOptions.Multiline 
    );
    
    var packageId = Environment.GetEnvironmentVariable("INPUT_PACKAGE_ID");

    if (packageId is null)
    {
        var packageRegex = new Regex(
            Environment.GetEnvironmentVariable("INPUT_PACKAGE_REGEX")
            ?? throw new ArgumentNullException("INPUT_PACKAGE_REGEX"),
            RegexOptions.Multiline 
        );

        var packageMatch = packageRegex.Match(file);

        packageId = packageMatch.Success ? packageMatch.Groups[1].Value : projectFile.Name.Replace(".csproj",string.Empty);
    }
    
    var versionMatch = versionPattern.Match(file);
    Version currentProjectVersion;
    
    if (versionMatch.Success)
        currentProjectVersion = Version.Parse(versionMatch.Groups[1].Value);
    else
    {
        Console.WriteLine("::warning file=Program.cs,title=Project version not found::We couldn't identify the version of the project, defaulting to 1.0.0");
        currentProjectVersion = Version.Parse("1.0.0");
    }
    
    var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
    var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

    var remotePackageVersions = await resource.GetAllVersionsAsync(
        packageId,
        new NullSourceCacheContext(),
        NullLogger.Instance,
        default
    );

    var nuGetVersions = remotePackageVersions as NuGetVersion[] ?? remotePackageVersions.ToArray();
    if (nuGetVersions.Any(t => t.Version == currentProjectVersion))
        throw new ApplicationException($"The version {currentProjectVersion} is already in use!");
}
catch (Exception e)
{
    Console.WriteLine($"::error file=Program.cs, title={e.Source}::{e.Message}");
}

