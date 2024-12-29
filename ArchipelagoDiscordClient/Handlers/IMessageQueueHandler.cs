
namespace ArchipelagoDiscordClient.Handlers
{
	public interface IMessageQueueHandler
	{
		Task ProcessMessageQueueAsync();
	}
}