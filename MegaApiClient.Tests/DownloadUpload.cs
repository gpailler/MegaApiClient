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
  public abstract class DownloadUpload : TestsBase, IDisposable
  {
    protected readonly Random random = new Random();

    private readonly int savedChunksPackSize;

    protected DownloadUpload(ITestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
      this.savedChunksPackSize = this.context.Options.ChunksPackSize;
    }

    public virtual void Dispose()
    {
      this.context.Options.ChunksPackSize = this.savedChunksPackSize;
    }

    [Theory, MemberData(nameof(InvalidUploadStreamParameters))]
    public void UploadStream_InvalidParameters_Throws(Stream stream, string name, INode parent, Type expectedExceptionType)
    {
      Assert.Throws(expectedExceptionType, () => this.context.Client.Upload(stream, name, parent));
    }

    public static IEnumerable<object[]> InvalidUploadStreamParameters
    {
      get
      {
        INode nodeDirectory = Mock.Of<INode>(x => x.Type == NodeType.Directory);
        INode nodeFile = Mock.Of<INode>(x => x.Type == NodeType.File);
        Stream stream = new MemoryStream();

        yield return new object[] {null, null, null, typeof(ArgumentNullException)};
        yield return new object[] {null, null, nodeDirectory, typeof(ArgumentNullException)};
        yield return new object[] {null, "", null, typeof(ArgumentNullException)};
        yield return new object[] {null, "", nodeDirectory, typeof(ArgumentNullException)};
        yield return new object[] {null, "name", null, typeof(ArgumentNullException)};
        yield return new object[] {null, "name", nodeDirectory, typeof(ArgumentNullException)};
        yield return new object[] {stream, null, null, typeof(ArgumentNullException)};
        yield return new object[] {stream, null, nodeDirectory, typeof(ArgumentNullException)};
        yield return new object[] {stream, "", null, typeof(ArgumentNullException)};
        yield return new object[] {stream, "", nodeDirectory, typeof(ArgumentNullException)};
        yield return new object[] {stream, "name", null, typeof(ArgumentNullException)};
        yield return new object[] {stream, "name", nodeFile, typeof(ArgumentException)};
      }
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void UploadStream_DifferentParent_Succeeds(NodeType parentType)
    {
      byte[] data = new byte[123];
      this.random.NextBytes(data);

      INode parent = this.GetNode(parentType);

      INode node;
      using (Stream stream = new MemoryStream(data))
      {
        node = this.context.Client.Upload(stream, "test", parent);
      }

      Assert.NotNull(node);
      Assert.Equal(NodeType.File, node.Type);
      Assert.Equal(parent.Id, node.ParentId);
      Assert.Equal("test", node.Name);
      Assert.Equal(data.Length, node.Size);
      Assert.Single(this.context.Client.GetNodes(), x => x.Id == node.Id);
    }

    [Theory]
    [InlineData(20000, 128*1024, 1)]
    [InlineData(200000, 128*1024, 2)]
    [InlineData(2000000, 128*1024, 6)]
    [InlineData(20000, 1024*1024, 1)]
    [InlineData(200000, 1024*1024, 1)]
    [InlineData(2000000, 1024*1024, 2)]
    [InlineData(2000000, -1, 1)]
    public void UploadStream_ValidateContent_Succeeds(int dataSize, int chunksPackSize, int expectedUploadCalls)
    {
      byte[] uploadedData = new byte[dataSize];
      this.random.NextBytes(uploadedData);

      INode parent = this.GetNode(NodeType.Root);

      using (Stream stream = new MemoryStream(uploadedData))
      {
        int uploadCalls = 0;
        Action<TestWebClient.CallType, Uri> onCall = (callType, url) =>
        {
          if (callType == TestWebClient.CallType.PostRequestRaw)
          {
            if (url.AbsolutePath.EndsWith("/0", StringComparison.Ordinal))
            {
              // Reset counter when it's the first chunk (to avoid error when Upload is restarted from start)
              uploadCalls = 1;
            }
            else
            {
              uploadCalls++;
            }
          }
        };

        ((TestWebClient)this.context.WebClient).OnCalled += onCall;

        this.context.Options.ChunksPackSize = chunksPackSize;
        var node = this.context.Client.Upload(stream, "test", parent);

        stream.Position = 0;
        this.AreStreamsEquivalent(this.context.Client.Download(node), stream);
        Assert.Equal(expectedUploadCalls, uploadCalls);
      }
    }

    [Theory, MemberData(nameof(DownloadLinkInvalidParameter))]
    public void DownloadLink_ToStream_InvalidParameter_Throws(Uri uri, Type expectedExceptionType)
    {
      Assert.Throws(expectedExceptionType, () => this.context.Client.Download(uri));
    }

    public static IEnumerable<object[]> DownloadLinkInvalidParameter()
    {
      yield return new object[] { null, typeof(ArgumentNullException) };
      yield return new object[] { new Uri("http://www.example.com"), typeof(ArgumentException) };
      yield return new object[] { new Uri("https://mega.nz"), typeof(ArgumentException) };
      yield return new object[] { new Uri("https://mega.nz/#!axYS1TLL"), typeof(ArgumentException) };
      yield return new object[] { new Uri("https://mega.nz/#!axYS1TLL!"), typeof(ArgumentException) };
    }

    [Fact]
    public void DownloadLink_ToStream_Succeeds()
    {
      const string expectedResultFile = "Data/SampleFile.jpg";

      using (Stream stream = new FileStream(this.GetAbsoluteFilePath(expectedResultFile), FileMode.Open))
      {
        this.AreStreamsEquivalent(this.context.Client.Download(new Uri(AuthenticatedTestContext.FileLink)), stream);
      }
    }

    [Fact]
    public void Download_ValidateStream_Succeeds()
    {
      using (Stream stream = this.context.Client.Download(new Uri(AuthenticatedTestContext.FileLink)))
      {
        Assert.NotNull(stream);
        Assert.Equal(523265, stream.Length);
        Assert.True(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanTimeout);
        Assert.False(stream.CanWrite);
        Assert.Equal(0, stream.Position);
      }
    }

    [Theory, MemberData(nameof(DownloadLinkToFileInvalidParameter))]
    public void DownloadLink_ToFile_InvalidParameter_Throws(Uri uri, string outFile, Type expectedExceptionType)
    {
      Assert.Throws(expectedExceptionType, () => this.context.Client.DownloadFile(uri, outFile));
    }

    public static IEnumerable<object[]> DownloadLinkToFileInvalidParameter
    {
      get
      {
        string outFile = Path.GetTempFileName();

        yield return new object[] { null, null, typeof(ArgumentNullException) };
        yield return new object[] { null, outFile, typeof(ArgumentNullException) };
        yield return new object[] { new Uri("http://www.example.com"), outFile, typeof(ArgumentException) };
        yield return new object[] { new Uri("https://mega.nz"), outFile, typeof(ArgumentException) };
        yield return new object[] { new Uri("https://mega.nz/#!38JjRYIA"), outFile, typeof(ArgumentException) };
        yield return new object[] { new Uri("https://mega.nz/#!ulISSQIb!"), outFile, typeof(ArgumentException) };
        yield return new object[] { new Uri(AuthenticatedTestContext.FileLink), null, typeof(ArgumentNullException) };
        yield return new object[] { new Uri(AuthenticatedTestContext.FileLink), string.Empty, typeof(ArgumentNullException) };
        yield return new object[] { new Uri(AuthenticatedTestContext.FileLink), outFile, typeof(IOException) };
      }
    }

    [Fact]
    public void DownloadLink_ToFile_Succeeds()
    {
      const string expectedResultFile = "Data/SampleFile.jpg";

      string outFile = Path.GetTempFileName();
      File.Delete(outFile);
      this.context.Client.DownloadFile(new Uri(AuthenticatedTestContext.FileLink), outFile);

      Assert.Equal(File.ReadAllBytes(this.GetAbsoluteFilePath(expectedResultFile)), File.ReadAllBytes(outFile));
    }

    [Fact]
    public void GetNodesFromLink_Download_Succeeds()
    {
      const string expectedResultFile = "Data/SampleFile.jpg";
      var nodes = this.context.Client.GetNodesFromLink(new Uri(AuthenticatedTestContext.FolderLink));
      var node = nodes.Single(x => x.Name == "SharedFile.jpg");

      using (Stream stream = new FileStream(this.GetAbsoluteFilePath(expectedResultFile), FileMode.Open))
      {
        this.AreStreamsEquivalent(this.context.Client.Download(node), stream);
      }
    }

    [Fact]
    public void UpdateFile_Replace_Succeeds()
    {
      const string sampleFile = "Data/SampleFile.jpg";
      var root = this.GetNode(NodeType.Root);

      var uploadNode = this.context.Client.UploadFile(sampleFile, root);
      var updateNode = this.context.Client.UpdateFile(sampleFile, root, uploadNode, UpdateMode.Replace);

      var nodes = this.context.Client.GetNodes().ToList();
      uploadNode = this.GetNode(uploadNode.Id);
      Assert.True(nodes.Contains(uploadNode));
      Assert.True(uploadNode.ParentId == this.GetNode(NodeType.Trash).Id);

      Assert.True(nodes.Contains(updateNode));
      Assert.True(updateNode.ParentId == root.Id);
    }

    [Fact]
    public void UpdateFile_Duplicate_Succeeds()
    {
      const string sampleFile = "Data/SampleFile.jpg";
      var root = this.GetNode(NodeType.Root);

      var uploadNode = this.context.Client.UploadFile(sampleFile, root);
      var updateNode = this.context.Client.UpdateFile(sampleFile, root, uploadNode, UpdateMode.Duplicate);

      var nodes = this.context.Client.GetNodes().ToList();
      Assert.True(nodes.Contains(uploadNode));
      Assert.True(uploadNode.ParentId == root.Id);

      Assert.True(nodes.Contains(updateNode));
      Assert.True(uploadNode.ParentId == root.Id);
    }

    [Fact]
    public void UpdateFile_Version_Succeeds()
    {
      const string sampleFile = "Data/SampleFile.jpg";
      var root = this.GetNode(NodeType.Root);

      var uploadNode = this.context.Client.UploadFile(sampleFile, root);
      var updateNode = this.context.Client.UpdateFile(sampleFile, root, uploadNode, UpdateMode.Version);

      Assert.Equal(uploadNode, updateNode);

      var nodes = this.context.Client.GetNodes().ToList();
      Assert.True(nodes.Contains(updateNode));
    }

    protected void AreStreamsEquivalent(Stream stream1, Stream stream2)
    {
      byte[] stream1data = new byte[stream1.Length];
      byte[] stream2data = new byte[stream2.Length];

      int readStream1 = stream1.Read(stream1data, 0, stream1data.Length);
      Assert.Equal(stream1data.Length, readStream1);

      int readStream2 = stream2.Read(stream2data, 0, stream2data.Length);
      Assert.Equal(stream2data.Length, readStream2);

      Assert.Equal(stream1data, stream2data);
    }

    protected void AreFileEquivalent(string file1, string file2)
    {
      using (Stream stream1 = new FileStream(file1, FileMode.Open))
      {
        using (Stream stream2 = new FileStream(file2, FileMode.Open))
        {
          this.AreStreamsEquivalent(stream1, stream2);
        }
      }
    }

    protected string GetAbsoluteFilePath(string relativeFilePath)
    {
      var currentAssembly = this.GetType().GetTypeInfo().Assembly.Location;
      var assemblyDirectory = Path.GetDirectoryName(currentAssembly);

      return Path.Combine(assemblyDirectory, relativeFilePath);
    }
  }
}
