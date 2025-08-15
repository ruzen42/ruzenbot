using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunnerExecute;

public interface IShellRunnerHttp 
{
   public Task<CommandResponse> Execute(CommandRequest request, CancellationToken cancellationToken);
}