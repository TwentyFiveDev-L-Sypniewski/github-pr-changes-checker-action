using Xunit;
using System.Net.Http;
using GithubPrChangesChecker;
using Moq;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System;
using System.Text;
using FluentAssertions;

namespace GithubPrChangesCheckerTests;

public class GithubClientTests
{

    private readonly GithubClient _sut;
    private readonly Mock<HttpMessageHandler> _messageHandlerMock;
    public GithubClientTests()
    {
        _messageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_messageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://example.com")
        };
        _sut = new GithubClient(httpClient);
    }

    [Fact]
    public async Task GetResponse_ReturnsProjectFolderName_WhenSingleFileWasChanged()
    {
        var content = GenerateTestData("MySingleProject/abc/def/code.cs");

        _messageHandlerMock.Protected()
       .SetupSequence<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        })
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[]")
        });

        var results = await _sut.GetChangedProjectsNames(default!, default!, default!, default!);
        results.Should().BeEquivalentTo(new[] { "MySingleProject" });
    }

    [Fact]
    public async Task GetResponse_ReturnsDistinctProjectNames_WhenMoreThanOneFileWasModifiedInProject()
    {
        var content = GenerateTestData(
            "MySingleProject/abc/def/main.cs",
            "MySingleProject/abc/def/program.cs",
            "AnotherProject/abc/def/index.html",
            "AnotherProject/abc/def/styles.css");

        _messageHandlerMock.Protected()
       .SetupSequence<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content)
        })
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[]")
        });

        var results = await _sut.GetChangedProjectsNames(default!, default!, default!, default!);
        results.Should().BeEquivalentTo(new[] { "MySingleProject", "AnotherProject" });
    }

    [Fact]
    public async Task GetResponse_ReturnsProjectNames_WhenResultsAreRetrievedFromMultipleApiPages()
    {
        var contentFromPage1 = GenerateTestData(
            "MySingleProject_page1/abc/def/main.cs",
            "MySingleProject_page1/abc/def/program.cs",
            "AnotherProject_page1/abc/def/index.html");

        var contentFromPage2 = GenerateTestData(
        "MySingleProject_page2/abc/def/main.cs",
        "SomeOther/def/program.cs",
        "AnotherProject_page2/abc/def/index.html");

        var contentFromPage3 = GenerateTestData("SuperSecretProjects_page3/abc/def/main.cs");

        _messageHandlerMock.Protected()
       .SetupSequence<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(contentFromPage1)
        })
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(contentFromPage2)
        })
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(contentFromPage3)
        })
        .ReturnsAsync(new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[]")
        });

        var results = await _sut.GetChangedProjectsNames(default!, default!, default!, default!);
        results.Should().BeEquivalentTo(new[] {
            "MySingleProject_page1",
             "AnotherProject_page1",
             "MySingleProject_page2",
             "SomeOther",
             "AnotherProject_page2",
             "SuperSecretProjects_page3"
             });
    }

    private static string GenerateTestData(int n)
    {
        var stringBuilder = new StringBuilder("[");
        for (var i = 0; i < n; i++)
        {
            stringBuilder.Append($"{{\"FileName\": \"project_{i}/subfolder1/subfolder2\"}}");
            if (i != n - 1)
            {
                stringBuilder.Append(',');
            }
        }
        stringBuilder.Append(']');
        return stringBuilder.ToString();

    }

    private static string GenerateTestData(params string[] filenames)
    {
        var stringBuilder = new StringBuilder("[");
        for (var i = 0; i < filenames.Length; i++)
        {
            stringBuilder.Append($"{{\"FileName\": \"{filenames[i]}\"}}");
            if (i != filenames.Length - 1)
            {
                stringBuilder.Append(',');
            }
        }
        stringBuilder.Append(']');
        return stringBuilder.ToString();

    }
}