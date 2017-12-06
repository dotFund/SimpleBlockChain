
namespace SimpleBlockchain.VM
{
    public interface IScriptContainer : IInteropInterface
    {
        byte[] GetMessage();
    }
}
