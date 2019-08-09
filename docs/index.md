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

* Only compatible with Mono Scripting Backend and recompiled version of JSON .NET For Unity to change the version from 8.3 to 10, not compatible with IL2CPP since IL2CPP is missing System.Web.HttpUtility.ParseQueryString method.
---

MegaApiClient source is available on [GitHub](https://github.com/gpailler/MegaApiClient) and is released under [MIT](https://choosealicense.com/licenses/mit/) license.
