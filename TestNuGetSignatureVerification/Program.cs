// See https://aka.ms/new-console-template for more information

using Microsoft.Build.Locator;
using TestNuGetSignatureVerification;

var instance = MSBuildLocator.RegisterDefaults();
Console.WriteLine($"Using MSBuild:{instance.MSBuildPath}");

var verifier = new PackageVerifier();
var degreeOfParallelism = args.Length >= 1 ? Convert.ToInt32(args[0]) : 1;
await verifier.VerifySignatures(degreeOfParallelism);
