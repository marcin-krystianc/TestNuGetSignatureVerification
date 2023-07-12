using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;

namespace TestNuGetSignatureVerification;

public class PackageVerifier2
{
    public async Task VerifySignatures(int degreeOfParallelism)
    {
        var nugetS = global::NuGet.Configuration.Settings.LoadDefaultSettings(null);
        var globalPackages = SettingsUtility.GetGlobalPackagesFolder(nugetS);
  
        var packages = Directory
            .EnumerateFiles(globalPackages, "*.nupkg", SearchOption.AllDirectories)
            .ToList();

        var provider = new SignatureTrustAndValidityVerificationProvider();
        var verifierSettings = new SignedPackageVerifierSettings
            (
                allowUnsigned:true,
                allowIllegal:true,
                allowUntrusted:true,
                allowIgnoreTimestamp:true,
                allowMultipleTimestamps:true,
                allowNoTimestamp:true,
                allowUnknownRevocation:true,
                reportUnknownRevocation:false,
                verificationTarget: VerificationTarget.All,
                signaturePlacement: SignaturePlacement.Any,
                repositoryCountersignatureVerificationBehavior: SignatureVerificationBehavior.IfExists,
                revocationMode: RevocationMode.Offline
            );
        
        Console.WriteLine($"Found {packages.Count} packages in '{globalPackages}");
        
        var sw = Stopwatch.StartNew();
        foreach (var packagePath in packages)
        {
            using (var packageReader = new PackageArchiveReader(packagePath))
            {
                var isSigned = await packageReader.IsSignedAsync(CancellationToken.None);
                if (isSigned)
                {
                    var signature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                    
                    var timestampCms = new SignedCms();
         
                    
                    // TODO 
                    // RSA? publicKey = certificate.GetRSAPublicKey();
                    
                    var result = await provider.GetTrustResultAsync(packageReader,signature, verifierSettings, CancellationToken.None);
                    {
                    }
                }
            }
        }
/*
        var tasks = packages.AsParallel().WithDegreeOfParallelism(degreeOfParallelism)
            .Select(async packagePath =>
            {
                using (var packageReader = new PackageArchiveReader(packagePath))
                {
                    var isSigned = await packageReader.IsSignedAsync(CancellationToken.None);
                    if (isSigned)
                    {
                        var signature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);

                        await provider.GetTrustResultAsync(packageReader,signature, verifierSettings, CancellationToken.None);
                    }
                }
            }).ToList();

        await Task.WhenAll(tasks);
*/
        sw.Stop();
        Console.WriteLine($"PackageVerifier2: Verified {packages.Count} packages in '{sw.Elapsed.TotalSeconds} seconds with degree of parallelism={degreeOfParallelism}" );
    }
}