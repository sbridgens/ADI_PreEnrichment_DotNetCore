namespace Application.DataAccess.Persistence.Contracts
{
    public interface ILatestUpdateStore
    {
        void InUpdateOperation(bool inOperation);
    }
}