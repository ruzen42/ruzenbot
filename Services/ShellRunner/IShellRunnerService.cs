using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunner;

public interface IShellRunnerService 
{
   Task<QueryShellResponse> Execute(string command, CancellationToken cancellationToken);
}