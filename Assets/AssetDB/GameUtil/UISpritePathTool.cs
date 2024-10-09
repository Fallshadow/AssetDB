public static class UISpritePathTool {
    private const string SpritePrePath = "asset://arts/ui/";

    public static string GetCommonIcon(string spriteName) {
        return $"{SpritePrePath}/Common/{spriteName}";
    }
}