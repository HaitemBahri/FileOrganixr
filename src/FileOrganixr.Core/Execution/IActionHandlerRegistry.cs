


using FileOrganixr.Core.Configuration.Definitions;

namespace FileOrganixr.Core.Execution;
public interface IActionHandlerRegistry
{

    IActionHandler Resolve(IActionDefinition actionDefinition);
}
