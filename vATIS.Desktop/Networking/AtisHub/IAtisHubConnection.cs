using System.Threading.Tasks;

namespace Vatsim.Vatis.Networking.AtisHub;

public interface IAtisHubConnection
{
    Task Connect();
    Task Disconnect();
    Task PublishAtis(AtisHubDto dto);
    Task SubscribeToAtis(SubscribeDto dto);
}