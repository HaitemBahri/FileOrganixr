using FileOrganixr.Core.Configuration.Definitions;
using FileOrganixr.Core.Configuration.Definitions.Actions;
using FileOrganixr.Core.Configuration.Definitions.Queries;
using FileOrganixr.Core.Configuration.Validators;
using FileOrganixr.Core.Configuration.Validators.ActionValidators;
using FileOrganixr.Core.Configuration.Validators.QueryValidator;
using FileOrganixr.Core.Configuration.Validators.RuleValidators;

namespace FileOrganixr.Tests.Configuration.Validators;
public sealed class RuleDefinitionValidatorTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenActionAndQueryAreMissing()
    {
        var actionRegistry = new StubActionDefinitionValidatorRegistry();
        var queryRegistry = new StubQueryDefinitionValidatorRegistry();
        var sut = new RuleDefinitionValidator(actionRegistry, queryRegistry);
        var rule = new RuleDefinition
        {
            Name = "Rule",
            Action = null!,
            Query = null!
        };

        var result = sut.Validate(rule, "Folders[0].Rules[0]");

        Assert.Contains(result.Errors, issue => issue.Path == "Folders[0].Rules[0].Action");
        Assert.Contains(result.Errors, issue => issue.Path == "Folders[0].Rules[0].Query");
    }

    [Fact]
    public void Validate_ReturnsError_WhenActionValidatorIsNotRegistered()
    {
        var actionRegistry = new StubActionDefinitionValidatorRegistry
        {
            ResolveFunc = _ => null
        };
        var queryRegistry = new StubQueryDefinitionValidatorRegistry
        {
            ResolveFunc = _ => new PassThroughQueryValidator()
        };
        var sut = new RuleDefinitionValidator(actionRegistry, queryRegistry);
        var rule = new RuleDefinition
        {
            Name = "Move txt",
            Action = new MoveActionDefinition { DestinationPath = @"C:\Dest" },
            Query = new RegexFileNameQueryDefinition { Pattern = @"\.txt$" }
        };

        var result = sut.Validate(rule, "Folders[0].Rules[1]");

        Assert.Contains(
            result.Errors,
            issue => issue.Path == "Folders[0].Rules[1].Action.Type" && issue.Message.Contains("No validator"));
    }

    [Fact]
    public void Validate_DelegatesToResolvedActionAndQueryValidators()
    {
        var actionValidator = new RecordingActionValidator();
        var queryValidator = new RecordingQueryValidator();
        var actionRegistry = new StubActionDefinitionValidatorRegistry
        {
            ResolveFunc = _ => actionValidator
        };
        var queryRegistry = new StubQueryDefinitionValidatorRegistry
        {
            ResolveFunc = _ => queryValidator
        };
        var sut = new RuleDefinitionValidator(actionRegistry, queryRegistry);
        var rule = new RuleDefinition
        {
            Name = "Rule",
            Action = new DeleteActionDefinition(),
            Query = new RegexFileNameQueryDefinition { Pattern = ".*" }
        };

        var result = sut.Validate(rule, "Folders[0].Rules[0]");

        Assert.Equal(1, actionValidator.Calls);
        Assert.Equal(1, queryValidator.Calls);
        Assert.Contains(result.Errors, issue => issue.Path == "Folders[0].Rules[0].Action.Custom");
        Assert.Contains(result.Errors, issue => issue.Path == "Folders[0].Rules[0].Query.Custom");
    }

    private sealed class StubActionDefinitionValidatorRegistry : IActionDefinitionValidatorRegistry
    {
        public Func<string, IActionDefinitionValidator?> ResolveFunc { get; set; } = _ => null;

        public IActionDefinitionValidator? Resolve(string actionType)
        {
            return ResolveFunc(actionType);
        }
    }

    private sealed class StubQueryDefinitionValidatorRegistry : IQueryDefinitionValidatorRegistry
    {
        public Func<IQueryDefinition, IQueryDefinitionValidator?> ResolveFunc { get; set; } = _ => null;

        public IQueryDefinitionValidator? Resolve(IQueryDefinition query)
        {
            return ResolveFunc(query);
        }
    }

    private sealed class PassThroughQueryValidator : IQueryDefinitionValidator
    {
        public string SupportedType => "RegexFileName";

        public ValidationResult Validate(IQueryDefinition query, string basePath)
        {
            return ValidationResult.Empty();
        }
    }

    private sealed class RecordingActionValidator : IActionDefinitionValidator
    {
        public int Calls { get; private set; }

        public string SupportedType => "Delete";

        public ValidationResult Validate(IActionDefinition action, string basePath)
        {
            Calls++;
            var result = ValidationResult.Empty();
            result.AddError($"{basePath}.Custom", "Action validator called.");
            return result;
        }
    }

    private sealed class RecordingQueryValidator : IQueryDefinitionValidator
    {
        public int Calls { get; private set; }

        public string SupportedType => "RegexFileName";

        public ValidationResult Validate(IQueryDefinition query, string basePath)
        {
            Calls++;
            var result = ValidationResult.Empty();
            result.AddError($"{basePath}.Custom", "Query validator called.");
            return result;
        }
    }
}
