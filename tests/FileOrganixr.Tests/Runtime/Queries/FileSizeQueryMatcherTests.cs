using System;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.Core.Runtime.FileContexts;
using FileOrganixr.Core.Runtime.Queries;

namespace FileOrganixr.Tests.Runtime.Queries;
public sealed class FileSizeQueryMatcherTests
{
    [Fact]
    public void IsMatch_ReturnsTrue_WhenFileSizeIsWithinInclusiveRange()
    {
        var matcher = new FileSizeQueryMatcher();
        var query = new FileSizeQueryDefinition
        {
            MinSize = 10m,
            MaxSize = 20m
        };
        var file = CreateFileContext(10);

        var result = matcher.IsMatch(query, file);

        Assert.True(result);
    }

    [Fact]
    public void IsMatch_ReturnsFalse_WhenFileSizeIsBelowMinimum()
    {
        var matcher = new FileSizeQueryMatcher();
        var query = new FileSizeQueryDefinition
        {
            MinSize = 100m,
            MaxSize = 200m
        };
        var file = CreateFileContext(99);

        var result = matcher.IsMatch(query, file);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ReturnsFalse_WhenFileSizeIsAboveMaximum()
    {
        var matcher = new FileSizeQueryMatcher();
        var query = new FileSizeQueryDefinition
        {
            MinSize = 100m,
            MaxSize = 200m
        };
        var file = CreateFileContext(201);

        var result = matcher.IsMatch(query, file);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_ReturnsFalse_WhenFileSizeIsUnknown()
    {
        var matcher = new FileSizeQueryMatcher();
        var query = new FileSizeQueryDefinition
        {
            MinSize = 1m,
            MaxSize = 10m
        };
        var file = CreateFileContext(null);

        var result = matcher.IsMatch(query, file);

        Assert.False(result);
    }

    [Fact]
    public void IsMatch_Throws_WhenQueryTypeIsUnexpected()
    {
        var matcher = new FileSizeQueryMatcher();
        var query = new RegexFileNameQueryDefinition
        {
            Pattern = ".*"
        };
        var file = CreateFileContext(50);

        Assert.Throws<ArgumentException>(() => matcher.IsMatch(query, file));
    }

    private static FileContext CreateFileContext(long? sizeBytes)
    {
        return new FileContext
        {
            FullPath = @"C:\tmp\file.txt",
            FileName = "file.txt",
            FileNameWithoutExtension = "file",
            Extension = ".txt",
            SizeBytes = sizeBytes
        };
    }
}
