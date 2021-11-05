# **MegaApiClient**'s documentation

**MegaApiClient is a C# .Net library to access [http://mega.co.nz](http://mega.co.nz) / [http://mega.nz](http://mega.nz) cloud storage and file hosting service.**


#### Documentation

Getting started with MegaApiClient
  * [Installation](xref:installation)
  * [Code samples](xref:samples)
  * [FAQ](xref:faq)
  * [API](xref:CG.Web.MegaApiClient.MegaApiClient) documentation


#### Compatibility

The library is built for the following targets: .NET 4.0, .NET 4.5, .NET 4.6, .NET 4.7, .NET 4.7.1, .NET Standard 1.3 and .NET Standard 2.0.
Here is the list of all supported implementations:

| Implementation             | Version   |
|----------------------------|-----------|
| .NET Framework             | 4.0       |
| .NET Framework             | >= 4.5    |
| .NET Core / .NET           | >= 1.0    |
| Mono                       | >= 4.6    |
| Xamarin.iOS                | >= 10.0   |
| Xamarin.Mac                | >= 3.0    |
| Xamarin.Android            | >= 7.0    |
| Universal Windows Platform | >= 10.0   |
| Unity*                     | >= 2018.2 |

---
⚠️ TLS 1.2 support should be enforced when using .NET Framework <= 4.7 or all the API calls to Mega will hang.
- For .NET Framework from 4.5 to 4.7.0, add `ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;`
- For .NET Framework 4, add `ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;` (.NET 4.5 should be installed on the machine).
- Alternatively, you can add relevant switches in your app.config https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls  

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
