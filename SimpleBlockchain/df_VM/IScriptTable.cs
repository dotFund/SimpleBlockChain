namespace SimpleBlockchain.VM
{
    public interface IScriptTable
    {
        byte[] GetScript(byte[] script_hash);
    }
}
