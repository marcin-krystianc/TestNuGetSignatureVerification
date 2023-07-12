using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;

namespace TestNuGetSignatureVerification;

public class PackageVerifier3
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

        var rsaCount = 0;
        var rsaTimestampCount = 0;
        
        var sw = Stopwatch.StartNew();
        foreach (var packagePath in packages)
        {
            using (var packageReader = new PackageArchiveReader(packagePath))
            {
                var isSigned = await packageReader.IsSignedAsync(CancellationToken.None);
                if (isSigned)
                {
                    var primarySignature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                    if (primarySignature is RepositoryPrimarySignature repositoryPrimarySignature)
                    {
                        var certificate = repositoryPrimarySignature.SignerInfo.Certificate;
                   
                        RSA? publicKey = certificate?.GetRSAPublicKey();
                        if (publicKey != null)
                        {
                            rsaCount++;
                        }
                    }
                    
                    Signature signature = primarySignature;
                    CryptographicAttributeObjectCollection unsignedAttributes = signature.SignerInfo.UnsignedAttributes;
                    foreach (CryptographicAttributeObject attribute in unsignedAttributes)
                    {
                        if (string.Equals(attribute.Oid.Value, Oids.SignatureTimeStampTokenAttribute, StringComparison.Ordinal))
                        {
                            foreach (AsnEncodedData value in attribute.Values)
                            {
                                var timestampCms = new SignedCms();
                                timestampCms.Decode(value.RawData);

                                foreach (var signerInfo in timestampCms.SignerInfos)
                                {
                                    var certificate = signerInfo.Certificate;
                                    RSA? publicKey = certificate?.GetRSAPublicKey();
                                    if (publicKey != null)
                                    {
                                        rsaTimestampCount++;
                                    }
                                }

                                timestampCms.CheckSignature(true);
                            }
                        }
                    }
                }
            }
        }

        sw.Stop();
        Console.WriteLine($"PackageVerifier3: Processed {packages.Count} packages in '{sw.Elapsed.TotalSeconds}, count(GetRSAPublicKey)={rsaCount}, count(TimestampRsa)={rsaTimestampCount}" );
    }
}