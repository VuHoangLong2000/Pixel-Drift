public interface IPoolable
{
    void OnSpawn();
    void OnGet();
    void OnRelease();
}