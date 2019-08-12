# **MegaApiClient**'s documentation

**MegaApiClient is a C# .Net library to access [http://mega.co.nz](http://mega.co.nz) / [http://mega.nz](http://mega.nz) cloud storage and file hosting service.**


#### Documentation

Getting started with MegaApiClient
  * [Installation](xref:installation)
  * [Code samples](xref:samples)
  * [FAQ](xref:faq)

Whole [API](xref:CG.Web.MegaApiClient.MegaApiClient) documentation


#### Compatibility

The library supports .NET 4.0, .NET 4.5, .NET 4.6 and [.NET Standard 1.3](https://docs.microsoft.com/en-us/dotnet/standard/net-standard). Here is the list of all supported implementations:

| Implementation             | Version   |
|----------------------------|-----------|
| .NET Framework             | 4.0       |
| .NET Framework             | >= 4.5    |
| .NET Core                  | >= 1.0    |
| Mono                       | >= 4.6    |
| Xamarin.iOS                | >= 10.0   |
| Xamarin.Mac                | >= 3.0    |
| Xamarin.Android            | >= 7.0    |
| Universal Windows Platform | >= 10.0   |
| Unity*                     | >= 2018.2 |

* Need a link.xml file and a recompiled version of JSON .NET for Unity if IL2CPP is used:
https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347
(get the source code from the zip and open it in Visual studio, change the version to 10.0.0.0 and add a reference to UnityEngine.dll in the Unity installation \Editor\Data\Managed. recompile and use the compiled .dll instead of the default dll that comes with MegaApiClient)
This is the link.xml file:
<linker>
    <assembly fullname="MegaApiClient"> // the name of the assembly
        <type fullname="CG.Web.MegaApiClient.*" preserve="all"/> // excludes all namespaces and classes recursively under MyNamespace
    </assembly>
</linker>

---

MegaApiClient source is available on [GitHub](https://github.com/gpailler/MegaApiClient) and is released under [MIT](https://choosealicense.com/licenses/mit/) license.
