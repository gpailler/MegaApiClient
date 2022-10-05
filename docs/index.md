# **MegaApiClient**'s documentation

**MegaApiClient is a C# .Net library to access [http://mega.co.nz](http://mega.co.nz) / [http://mega.nz](http://mega.nz) cloud storage and file hosting service.**


#### Documentation

Getting started with MegaApiClient
  * [Installation](xref:installation)
  * [Code samples](xref:samples)
  * [FAQ](xref:faq)
  * [API](xref:CG.Web.MegaApiClient.MegaApiClient) documentation


#### Compatibility

The library is built for the following targets: .NET 4.7.2, .NET Standard 2.0 and .NET 6.0.
Here is the list of all supported implementations:

| Implementation             | Version         |
|----------------------------|-----------------|
| .NET Framework             | >= 4.7.2        |
| .NET Core / .NET           | >= 2.0          |
| Mono                       | >= 5.4, 6.4     |
| Xamarin.iOS                | >= 10.14, 12.16 |
| Xamarin.Mac                | >= 3.8, 5.6     |
| Xamarin.Android            | >= 8.0, 10.0    |
| Universal Windows Platform | >= 10.0.16299   |
| Unity*                     | >= 2018.1       |


---
\* Need a link.xml file if IL2CPP is used:
```
<linker>
    <!-- the name of the assembly -->
    <assembly fullname="MegaApiClient">
        <!-- excludes all namespaces and classes recursively under MyNamespace -->
        <type fullname="CG.Web.MegaApiClient.*" preserve="all"/>
    </assembly>
</linker>
```
---

MegaApiClient source is available on [GitHub](https://github.com/gpailler/MegaApiClient) and is released under [MIT](https://choosealicense.com/licenses/mit/) license.
