using System.Diagnostics;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace TestNuGetSignatureVerification;

public class PackageVerifier
{
    public static Task CopyToAsync(Stream stream)
    {
        return Task.CompletedTask;
    }

    public async Task VerifySignatures(int degreeOfParallelism)
    {
        var providers = Repository.Provider.GetCoreV3();
        var settings = global::NuGet.Configuration.Settings.LoadDefaultSettings(null);
        var globalPackages = SettingsUtility.GetGlobalPackagesFolder(settings);
        var sources = PackageSourceProvider.LoadPackageSources(settings);

        var mainSource = sources
            .Where(x => x.IsEnabled)
            .Where(x => x.IsHttp)
            .FirstOrDefault()
            .Name;

        var packages = Directory
            .EnumerateFiles(globalPackages, "*.nupkg", SearchOption.AllDirectories)
            .ToList();

        Console.WriteLine($"Found {packages.Count} packages in '{globalPackages}");
        var clientPolicyContext = ClientPolicyContext.GetClientPolicy(settings, NullLogger.Instance);

        var packagePathResolver = new PackagePathResolver(globalPackages);
        var packageExtractionContext = new PackageExtractionContext(PackageSaveMode.None, XmlDocFileSaveMode.None, clientPolicyContext, NullLogger.Instance);

        var sw = Stopwatch.StartNew();
        var tasks = packages.AsParallel().WithDegreeOfParallelism(degreeOfParallelism)
            .Select(async packagePath =>
            {
                using var stream = File.Open(packagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                await PackageExtractor.ExtractPackageAsync(mainSource, (Stream)stream, packagePathResolver, packageExtractionContext, CancellationToken.None);
            }).ToList();

        await Task.WhenAll(tasks);

        sw.Stop();
        Console.WriteLine($"Verified {packages.Count} packages in '{sw.Elapsed.TotalSeconds} seconds with degree of parallelism={degreeOfParallelism}" );
    }
}