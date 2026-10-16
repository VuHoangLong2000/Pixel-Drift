//Lưu DataType trong bộ DataType_AssetID_Field
public enum LocalizeDataType
{
    Character,
    Item,
    Equipment,
    Skill,
    Quest,
    Achievement,
}
public enum LocalizeField
{
    Name,
    Description,
    Detail,
    Title,
}
public static class LocalizeKey
{
    public static string Get(LocalizeDataType dataType, string assetId, LocalizeField field)
    {
        return $"{dataType.ToString().ToLower()}_{assetId}_{field}";
    }
}