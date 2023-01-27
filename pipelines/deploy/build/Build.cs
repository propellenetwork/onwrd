using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.PushPackage);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [Parameter("Version")]
    string Version { get; set; }

    [Parameter("Artifact Directory")]
    string ArtifactDirectory { get; set; }

    [Parameter("NuGet API Key")]
    string NuGetApiKey { get; set; }

    [Parameter("Skip NuGet Push")]
    bool SkipNuGetPush { get; set; }

    Target Initialize => _ => _
        .Executes(() =>
        {
            Console.WriteLine($"{nameof(ArtifactDirectory)}: {ArtifactDirectory}");
            Console.WriteLine($"{nameof(NugetArtifactsDirectory)}: {NugetArtifactsDirectory}");
            Console.WriteLine($"{nameof(ProjectDirectories)}: {ProjectDirectories.Select(x => $"\r\n  - {x}").Aggregate((prev, curr) => $"{prev}{curr}")}");
            Console.WriteLine($"{nameof(Configuration)}: {Configuration}");
            Console.WriteLine($"{nameof(Version)}: {Version}");
            Console.WriteLine($"{nameof(NuGetApiKey)}: {(!string.IsNullOrWhiteSpace(NuGetApiKey) ? "********" : "EMPTY" )}");
        });

    Target Clean => _ => _
        .DependsOn(Initialize)
        .Executes(() =>
        {
            EnsureCleanDirectory(NugetArtifactsDirectory);
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var projectDirectory in ProjectDirectories)
            {
                DotNet($"build -c {Configuration} -p:Version={Version}", workingDirectory: projectDirectory);
            }
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var projectDirectory in ProjectDirectories)
            {
                DotNet($"pack -c {Configuration} -p:PackageVersion={Version} --no-build --no-restore -o {NugetArtifactsDirectory}", workingDirectory: projectDirectory);
            }
        });

    Target PushPackage => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            if (SkipNuGetPush)
            {
                Console.WriteLine("Skipping NuGet push");
            }

            if (!NuGetPackages.Any())
            {
                Console.WriteLine("No NuGet Packages Found :(");
            }

            Console.WriteLine($"NuGet Packages Found: {NuGetPackages.Select(x => $"\r\n  - {x}").Aggregate((prev, curr) => $"{prev}{curr}")}");

            foreach (var package in NuGetPackages)
            {
                DotNet($"nuget push {package} -s https://api.nuget.org/v3/index.json -k {NuGetApiKey}", workingDirectory: package.Parent);
            }
        });

    AbsolutePath DefaultWorkingDirectory => RootDirectory / "../..";

    IEnumerable<AbsolutePath> ProjectDirectories => (DefaultWorkingDirectory / ArtifactDirectory).GlobDirectories("Onwrd.*");

    AbsolutePath NugetArtifactsDirectory => DefaultWorkingDirectory / ArtifactDirectory / "packages";

    IEnumerable<AbsolutePath> NuGetPackages => NugetArtifactsDirectory.GlobFiles("*.nupkg");
}