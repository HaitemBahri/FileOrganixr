using System;
using System.Collections.Generic;

namespace FileOrganixr.Core.Configuration.Validators;
public sealed class ValidationResult
{

    public List<ValidationIssue> Errors { get; } = new();


    public List<ValidationIssue> Warnings { get; } = new();


    public static ValidationResult Empty()
    {

        return new ValidationResult();
    }


    public void AddError(string path, string message)
    {

        Errors.Add(new ValidationIssue(path, message));
    }


    public void AddWarning(string path, string message)
    {

        Warnings.Add(new ValidationIssue(path, message));
    }


    public void Merge(ValidationResult other)
    {

        ArgumentNullException.ThrowIfNull(other);


        Errors.AddRange(other.Errors);


        Warnings.AddRange(other.Warnings);
    }
}
