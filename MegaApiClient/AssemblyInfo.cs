using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d4d9205c-f753-4694-8823-dac8a65729be")]

#if NETSTANDARD1_3 || NETSTANDARD2_0
[assembly: InternalsVisibleTo("MegaApiClient.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#else
[assembly: InternalsVisibleTo("MegaApiClient.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100592e623bfeb798f72e8e912283641da7e5b000a57c6d1ebaee470c3ec0fb4ebc6a4f643d02f41395e670276ec744e8d93667047d0cd04c5ad5c8015b53ceb4798ec1f8f0277d722e652a5a1aff7c2b502512c681988cffcd3cdbfdd56694fa5a518688a24cccdf52be4e47b271e4830162fbfbd3a85ed418f19c77c0fa1d498a")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
#endif
