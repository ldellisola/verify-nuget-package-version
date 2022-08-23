# verify-nuget-package-version
A github action that lets you know if you need to bump the package version of your library

## Inputs

### `PROJECT_FILE_PATH`

**Required** Path from the root directory to the project file where the version is defined.

### `VERSION_REGEX`
The regex pattern to extract the version defined in the project file. Defaults to `^\s*<PackageVersion>(.*)<\/PackageVersion>\s*$`

### `PACKAGE_ID`
It will default to the project's name. It will take precedence if both this parameter and `PACKAGE_REGEX` are defined.

### `PACKAGE_REGEX`
Regex pattern to extract the package id. It defaults to `^\s*<PackageId>(.*)</PackageId>\s*$`

## Example usage
```
uses: actions/verify-nuget-package-version@v1
with:
PROJECT_FILE_PATH: 'src/NugetLib.csproj'
PACKAGE_ID:  'NugetLib'
```
