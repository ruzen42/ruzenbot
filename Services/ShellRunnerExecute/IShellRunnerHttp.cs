using RuzenBot.Models.ShellRunner;

namespace RuzenBot.Services.ShellRunnerExecute;

public interface IShellRunnerHttp 
{
   public string Host { get; init; }
   public string Path { get; init; } 

   public Task<CommandResponse> Execute(CommandRequest request, CancellationToken cancellationToken);
}