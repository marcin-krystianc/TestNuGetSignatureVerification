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
                    /*
                    Signature signature = primarySignature;
                    var signerInfo = signature.SignerInfo;
                    CryptographicAttributeObjectCollection unsignedAttributes = signerInfo.UnsignedAttributes;
                    var timestampList = new List<Timestamp>();
                    foreach (CryptographicAttributeObject attribute in unsignedAttributes)
                    {
                        if (string.Equals(attribute.Oid.Value, Oids.SignatureTimeStampTokenAttribute, StringComparison.Ordinal))
                        {
                            foreach (AsnEncodedData value in attribute.Values)
                            {
                                var timestampCms = new SignedCms();

                                timestampCms.Decode(value.RawData);

                                using (var certificates = SignatureUtility.GetTimestampCertificates(
                                           timestampCms,
                                           SigningSpecifications.V1,
                                           "signature"))
                                {
                                    if (certificates == null || certificates.Count == 0)
                                    {
                                        throw new SignatureException(NuGetLogCode.NU3029, Strings.InvalidTimestampSignature);
                                    }
                                }

                                timestampList.Add(new Timestamp(timestampCms));
                            }
                        }
                    }
                    */
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"PackageVerifier3: Processed {packages.Count} packages in '{sw.Elapsed.TotalSeconds}, count(GetRSAPublicKey)={rsaCount}" );
    }
}