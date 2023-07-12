// See https://aka.ms/new-console-template for more information

using Microsoft.Build.Locator;
using TestNuGetSignatureVerification;

/*
var instance = MSBuildLocator.RegisterDefaults();
Console.WriteLine($"Using MSBuild:{instance.MSBuildPath}");
*/
MSBuildLocator.RegisterMSBuildPath(new[] { @"d:\dotnet\sdk\7.0.305\"});

var verifier = new PackageVerifier3();
var degreeOfParallelism = args.Length >= 1 ? Convert.ToInt32(args[0]) : 1;
await verifier.VerifySignatures(degreeOfParallelism);
