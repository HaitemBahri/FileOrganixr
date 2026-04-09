using System.Collections.Generic;
using FileOrganixr.Core.Configuration;
using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Validators;
using FileOrganixr.Core.Configuration.Validators.FolderValidators;

namespace FileOrganixr.Tests.Configuration.Validators;
public sealed class ConfigurationRootValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenSchemaVersionIsUnsupported()
    {
        var folderValidator = new StubFolderDefinitionValidator();
        var sut = new ConfigurationRootValidator(folderValidator);
        var root = new ConfigurationRoot
        {
            SchemaVersion = 2,
            Folders = []
        };

        var result = sut.Validate(root);

        Assert.Contains(result.Errors, issue => issue.Path == "SchemaVersion");
    }

    [Fact]
    public void Validate_ReturnsError_WhenFoldersCollectionIsMissing()
    {
        var folderValidator = new StubFolderDefinitionValidator();
        var sut = new ConfigurationRootValidator(folderValidator);
        var root = new ConfigurationRoot
        {
            Folders = null!
        };

        var result = sut.Validate(root);

        Assert.Contains(result.Errors, issue => issue.Path == "Folders" && issue.Message.Contains("missing"));
        Assert.Empty(folderValidator.BasePaths);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForDuplicateFolderNameAndPath_CaseInsensitive()
    {
        var folderValidator = new StubFolderDefinitionValidator();
        var sut = new ConfigurationRootValidator(folderValidator);
        var root = new ConfigurationRoot
        {
            Folders =
            [
                new FolderDefinition
                {
                    Name = "Inbox",
                    Path = @"C:\Files",
                    Rules = []
                },
                new FolderDefinition
                {
                    Name = "inbox",
                    Path = @"c:\files",
                    Rules = []
                }
            ]
        };

        var result = sut.Validate(root);

        Assert.Contains(result.Errors, issue => issue.Path == "Folders[1].Name" && issue.Message.Contains("Duplicate"));
        Assert.Contains(result.Errors, issue => issue.Path == "Folders[1].Path" && issue.Message.Contains("Duplicate"));
        Assert.Equal(["Folders[0]", "Folders[1]"], folderValidator.BasePaths);
    }

    [Fact]
    public void Validate_DelegatesToFolderValidator_AndMergesItsResult()
    {
        var folderValidator = new StubFolderDefinitionValidator();
        folderValidator.ResultFactory = (_, basePath) =>
        {
            var result = ValidationResult.Empty();
            result.AddError($"{basePath}.Rules", "Injected error.");
            return result;
        };

        var sut = new ConfigurationRootValidator(folderValidator);
        var root = new ConfigurationRoot
        {
            Folders =
            [
                new FolderDefinition
                {
                    Name = "Inbox",
                    Path = @"C:\Files",
                    Rules = []
                }
            ]
        };

        var result = sut.Validate(root);

        Assert.Contains(result.Errors, issue => issue.Path == "Folders[0].Rules" && issue.Message == "Injected error.");
        Assert.Equal(["Folders[0]"], folderValidator.BasePaths);
    }

    private sealed class StubFolderDefinitionValidator : IFolderDefinitionValidator
    {
        public List<string> BasePaths { get; } = [];

        public Func<FolderDefinition, string, ValidationResult> ResultFactory { get; set; } =
            (_, _) => ValidationResult.Empty();

        public ValidationResult Validate(FolderDefinition folder, string basePath)
        {
            BasePaths.Add(basePath);
            return ResultFactory(folder, basePath);
        }
    }
}
