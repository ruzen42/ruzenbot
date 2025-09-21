using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunnerExecute;

public interface IShellRunnerService 
{
   Task<CommandResponse> Execute(string request, CancellationToken cancellationToken);
}