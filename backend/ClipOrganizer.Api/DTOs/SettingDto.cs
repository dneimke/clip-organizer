namespace ClipOrganizer.Api.DTOs;

public class SettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class RootFolderSettingDto
{
    public string RootFolderPath { get; set; } = string.Empty;
}

