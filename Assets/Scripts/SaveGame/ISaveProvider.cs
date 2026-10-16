public interface ISaveProvider
{
    SaveContainer Load();

    void Save(SaveContainer container);

    bool Exists();

    void Delete();
}