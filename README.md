# TestNuGetSignatureVerification on Linux
``` <docker.exe run -t -i --rm ubuntu>
> apt-get update && apt-get install -y git dotnet7
> dotnet --info
> git clone https://github.com/marcin-krystianc/TestNuGetSignatureVerification.git
> git clone https://github.com/dotnet/orleans.git
> dotnet restore orleans/Orleans.sln
> export DOTNET_NUGET_SIGNATURE_VERIFICATION=True
> dotnet run -c Release --project TestNuGetSignatureVerification/TestNuGetSignatureVerification/TestNuGetSignatureVerification.csproj -- 1
Using MSBuild:/usr/lib/dotnet/sdk/7.0.107
Found 479 packages in '/root/.nuget/packages/
Verified 479 packages in '31.7100083 seconds with degree of parallelism=1

> dotnet nuget locals --list global-packages
> time dotnet nuget verify --all ~/.nuget/packages/**/*/*.nupkg -v diag > out.txt
real    0m47.733s
user    0m48.614s
sys     0m0.983s

```

# TestNuGetSignatureVerification on Windows (PowerShell)
```
> dotnet --info
> git clone https://github.com/marcin-krystianc/TestNuGetSignatureVerification.git
> git clone https://github.com/dotnet/orleans.git
> dotnet restore orleans/Orleans.sln
> dotnet run -c Release --project TestNuGetSignatureVerification/TestNuGetSignatureVerification/TestNuGetSignatureVerification.csproj -- 1
Using MSBuild:C:\Program Files\dotnet\sdk\7.0.304
Found 493 packages in 'C:\Users\Marcin\.nuget\packages\
Verified 493 packages in '4.1218048 seconds with degree of parallelism=1

> dotnet nuget locals --list global-packages
> Measure-Command {dotnet nuget verify --all $env:USERPROFILE\.nuget\packages\**\*\*.nupkg -v diag > out.txt}
...
Seconds           : 4
Milliseconds      : 638
...
```
