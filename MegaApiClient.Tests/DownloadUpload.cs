using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CG.Web.MegaApiClient.Tests.Context;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  using xRetry;

  public abstract class DownloadUpload : TestsBase
  {
    protected readonly Random Random = new();

    private readonly int _savedChunksPackSize;

    protected DownloadUpload(ITestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
      _savedChunksPackSize = Context.Options.ChunksPackSize;
    }

    public override void Dispose()
    {
      Context.Options.ChunksPackSize = _savedChunksPackSize;
      base.Dispose();
    }

    [Theory, MemberData(nameof(InvalidUploadStreamParameters))]
    public void UploadStream_InvalidParameters_Throws(bool hasStream, string name, NodeType? nodeType, Type expectedExceptionType)
    {
      var stream = hasStream ? new MemoryStream() : null;
      var parent = nodeType == null
        ? null
        : nodeType == NodeType.Directory
          ? Mock.Of<INode>(x => x.Type == NodeType.Directory)
          : Mock.Of<INode>(x => x.Type == NodeType.File);
      Assert.Throws(expectedExceptionType, () => Context.Client.Upload(stream, name, parent));
    }

    public static IEnumerable<object[]> InvalidUploadStreamParameters
    {
      get
      {
        yield return new object[] { false, null, null, typeof(ArgumentNullException) };
        yield return new object[] { false, null, NodeType.Directory, typeof(ArgumentNullException) };
        yield return new object[] { false, "", null, typeof(ArgumentNullException) };
        yield return new object[] { false, "", NodeType.Directory, typeof(ArgumentNullException) };
        yield return new object[] { false, "name", null, typeof(ArgumentNullException) };
        yield return new object[] { false, "name", NodeType.Directory, typeof(ArgumentNullException) };
        yield return new object[] { true, null, null, typeof(ArgumentNullException) };
        yield return new object[] { true, null, NodeType.Directory, typeof(ArgumentNullException) };
        yield return new object[] { true, "", null, typeof(ArgumentNullException) };
        yield return new object[] { true, "", NodeType.Directory, typeof(ArgumentNullException) };
        yield return new object[] { true, "name", null, typeof(ArgumentNullException) };
        yield return new object[] { true, "name", NodeType.File, typeof(ArgumentException) };
      }
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void UploadStream_DifferentParent_Succeeds(NodeType parentType)
    {
      var data = new byte[123];
      Random.NextBytes(data);

      var parent = GetNode(parentType);

      INode node;
      using (Stream stream = new MemoryStream(data))
      {
        node = Context.Client.Upload(stream, "test", parent);
      }

      Assert.NotNull(node);
      Assert.Equal(NodeType.File, node.Type);
      Assert.Equal(parent.Id, node.ParentId);
      Assert.Equal("test", node.Name);
      Assert.Equal(data.Length, node.Size);
      Assert.Single(Context.Client.GetNodes(), x => x.Id == node.Id);
    }

    [Theory]
    [InlineData(20000, 128 * 1024, 1)]
    [InlineData(200000, 128 * 1024, 2)]
    [InlineData(2000000, 128 * 1024, 6)]
    [InlineData(20000, 1024 * 1024, 1)]
    [InlineData(200000, 1024 * 1024, 1)]
    [InlineData(2000000, 1024 * 1024, 2)]
    [InlineData(2000000, -1, 1)]
    public void UploadStream_ValidateContent_Succeeds(int dataSize, int chunksPackSize, int expectedUploadCalls)
    {
      var uploadedData = new byte[dataSize];
      Random.NextBytes(uploadedData);

      var parent = GetNode(NodeType.Root);

      using Stream stream = new MemoryStream(uploadedData);
      var uploadCalls = 0;
      void OnCall(TestWebClient.CallType callType, Uri url)
      {
        if (callType == TestWebClient.CallType.PostRequestRaw)
        {
          uploadCalls++;
        }
      }

      Context.Options.ChunksPackSize = chunksPackSize;
      INode node = null;
      try
      {
        ((TestWebClient)Context.WebClient).OnCalled += OnCall;
        node = Context.Client.Upload(stream, "test", parent);
      }
      finally
      {
        ((TestWebClient)Context.WebClient).OnCalled -= OnCall;
        Assert.Equal(expectedUploadCalls, uploadCalls);
      }

      stream.Position = 0;
      AreStreamsEquivalent(Context.Client.Download(node), stream);
    }

    [Theory, MemberData(nameof(DownloadLinkInvalidParameter))]
    public void DownloadLink_ToStream_InvalidParameter_Throws(string uriString, Type expectedExceptionType)
    {
      var uri = uriString == null ? null : new Uri(uriString);
      Assert.Throws(expectedExceptionType, () => Context.Client.Download(uri));
    }

    public static IEnumerable<object[]> DownloadLinkInvalidParameter()
    {
      yield return new object[] { null, typeof(ArgumentNullException) };
      yield return new object[] { "http://www.example.com", typeof(ArgumentException) };
      yield return new object[] { "https://mega.nz", typeof(ArgumentException) };
      yield return new object[] { "https://mega.nz/#!axYS1TLL", typeof(ArgumentException) };
      yield return new object[] { "https://mega.nz/#!axYS1TLL!", typeof(ArgumentException) };
    }

    [Fact]
    public void DownloadLink_ToStream_Succeeds()
    {
      const string ExpectedResultFile = "Data/SampleFile.jpg";

      using Stream stream = new FileStream(GetAbsoluteFilePath(ExpectedResultFile), FileMode.Open, FileAccess.Read);
      AreStreamsEquivalent(Context.Client.Download(new Uri(AuthenticatedTestContext.Inputs.FileLink)), stream);
    }

    [RetryFact]
    public void Download_ValidateStream_Succeeds()
    {
      using var stream = Context.Client.Download(new Uri(AuthenticatedTestContext.Inputs.FileLink));
      Assert.NotNull(stream);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Size, stream.Length);
      Assert.True(stream.CanRead);
      Assert.False(stream.CanSeek);
      Assert.False(stream.CanTimeout);
      Assert.False(stream.CanWrite);
      Assert.Equal(0, stream.Position);
    }

    [Theory, MemberData(nameof(DownloadLinkToFileInvalidParameter))]
    public void DownloadLink_ToFile_InvalidParameter_Throws(string uriString, string outFile, Type expectedExceptionType)
    {
      var uri = uriString == null ? null : new Uri(uriString);
      Assert.Throws(expectedExceptionType, () => Context.Client.DownloadFile(uri, outFile));
    }

    public static IEnumerable<object[]> DownloadLinkToFileInvalidParameter
    {
      get
      {
        var outFile = Path.GetTempFileName();

        yield return new object[] { null, null, typeof(ArgumentNullException) };
        yield return new object[] { null, outFile, typeof(ArgumentNullException) };
        yield return new object[] { "http://www.example.com", outFile, typeof(ArgumentException) };
        yield return new object[] { "https://mega.nz", outFile, typeof(ArgumentException) };
        yield return new object[] { "https://mega.nz/#!38JjRYIA", outFile, typeof(ArgumentException) };
        yield return new object[] { "https://mega.nz/#!ulISSQIb!", outFile, typeof(ArgumentException) };
        yield return new object[] { AuthenticatedTestContext.Inputs.FileLink, null, typeof(ArgumentNullException) };
        yield return new object[] { AuthenticatedTestContext.Inputs.FileLink, string.Empty, typeof(ArgumentNullException) };
        yield return new object[] { AuthenticatedTestContext.Inputs.FileLink, outFile, typeof(IOException) };
      }
    }

    [Fact]
    public void DownloadLink_ToFile_Succeeds()
    {
      const string ExpectedResultFile = "Data/SampleFile.jpg";

      var outFile = GetTempFileName();
      Context.Client.DownloadFile(new Uri(AuthenticatedTestContext.Inputs.FileLink), outFile);

      Assert.Equal(File.ReadAllBytes(GetAbsoluteFilePath(ExpectedResultFile)), File.ReadAllBytes(outFile));
    }

    [Fact]
    public void GetNodesFromLink_Download_Succeeds()
    {
      const string ExpectedResultFile = "Data/SampleFile.jpg";
      var nodes = Context.Client.GetNodesFromLink(new Uri(AuthenticatedTestContext.Inputs.FolderLink));
      var node = nodes.Single(x => x.Name == "SharedFile.jpg");

      using Stream stream = new FileStream(GetAbsoluteFilePath(ExpectedResultFile), FileMode.Open, FileAccess.Read);
      AreStreamsEquivalent(Context.Client.Download(node), stream);
    }

    protected static void AreStreamsEquivalent(Stream stream1, Stream stream2)
    {
      var stream1data = new byte[stream1.Length];
      var stream2data = new byte[stream2.Length];

      var readStream1 = stream1.Read(stream1data, 0, stream1data.Length);
      Assert.Equal(stream1data.Length, readStream1);

      var readStream2 = stream2.Read(stream2data, 0, stream2data.Length);
      Assert.Equal(stream2data.Length, readStream2);

      Assert.Equal(stream1data, stream2data);
    }

    protected static void AreFileEquivalent(string file1, string file2)
    {
      using Stream stream1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
      using Stream stream2 = new FileStream(file2, FileMode.Open, FileAccess.Read);
      AreStreamsEquivalent(stream1, stream2);
    }

    protected string GetAbsoluteFilePath(string relativeFilePath)
    {
      var currentAssembly = GetType().GetTypeInfo().Assembly.Location;
      var assemblyDirectory = Path.GetDirectoryName(currentAssembly);

      return Path.Combine(assemblyDirectory, relativeFilePath);
    }

    protected static string GetTempFileName()
    {
      var file = Path.GetTempFileName();
      File.Delete(file);

      return file;
    }
  }
}
