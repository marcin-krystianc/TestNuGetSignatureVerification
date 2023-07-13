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

        Console.WriteLine($"Found {packages.Count} packages in '{globalPackages}");

        var rsaCount = 0;
        var rsaTimestampCount = 0;
        
        var sw = Stopwatch.StartNew();
        var rsaSw = new Stopwatch();
        var getCertificateSw = new Stopwatch();
        var getPrimarySignatureSw = new Stopwatch();
        foreach (var packagePath in packages)
        {
            using (var packageReader = new PackageArchiveReader(packagePath))
            {
                getPrimarySignatureSw.Start();
                var primarySignature = await packageReader.GetPrimarySignatureAsync(CancellationToken.None);
                getPrimarySignatureSw.Stop();
                
                if (primarySignature is RepositoryPrimarySignature repositoryPrimarySignature)
                {
                    var certificate = repositoryPrimarySignature.SignerInfo.Certificate;
               
                    rsaSw.Start();
                    RSA? publicKey = certificate?.GetRSAPublicKey();
                    rsaSw.Stop();
                    
                    if (publicKey != null)
                    {
                        rsaCount++;
                    }
                }

                if (primarySignature != null)
                {
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
                                    getCertificateSw.Start();
                                    var certificate = signerInfo.Certificate;
                                    getCertificateSw.Stop();
                                    
                                    rsaSw.Start();
                                    RSA? publicKey = certificate?.GetRSAPublicKey();
                                    rsaSw.Stop();

                                    if (publicKey != null)
                                    {
                                        rsaTimestampCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        sw.Stop();
        Console.WriteLine($"PackageVerifier3: Processed {packages.Count} packages in {sw.Elapsed.TotalSeconds}, PrimarySignature={getPrimarySignatureSw.Elapsed.TotalSeconds}, GetRSAPublicKey={rsaSw.Elapsed.TotalSeconds}, GetCertificate={getCertificateSw.Elapsed.TotalSeconds}, count(GetRSAPublicKey)={rsaCount}, count(TimestampRsa)={rsaTimestampCount}" );
    }
}