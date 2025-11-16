using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Tests.Helpers;

public class ClipBuilder
{
    private Clip _clip = new Clip();

    public ClipBuilder WithId(int id)
    {
        _clip.Id = id;
        return this;
    }

    public ClipBuilder WithTitle(string title)
    {
        _clip.Title = title;
        return this;
    }

    public ClipBuilder WithDescription(string description)
    {
        _clip.Description = description;
        return this;
    }

    public ClipBuilder WithStorageType(StorageType storageType)
    {
        _clip.StorageType = storageType;
        return this;
    }

    public ClipBuilder WithLocationString(string locationString)
    {
        _clip.LocationString = locationString;
        return this;
    }

    public ClipBuilder WithDuration(int durationSeconds)
    {
        _clip.Duration = durationSeconds;
        return this;
    }

    public ClipBuilder WithThumbnailPath(string? thumbnailPath)
    {
        _clip.ThumbnailPath = thumbnailPath;
        return this;
    }

    public ClipBuilder WithTags(params Tag[] tags)
    {
        _clip.Tags = tags.ToList();
        return this;
    }

    public ClipBuilder WithTag(Tag tag)
    {
        _clip.Tags.Add(tag);
        return this;
    }

    public Clip Build() => _clip;
}

public class TagBuilder
{
    private Tag _tag = new Tag();

    public TagBuilder WithId(int id)
    {
        _tag.Id = id;
        return this;
    }

    public TagBuilder WithCategory(TagCategory category)
    {
        _tag.Category = category;
        return this;
    }

    public TagBuilder WithValue(string value)
    {
        _tag.Value = value;
        return this;
    }

    public Tag Build() => _tag;
}

public class SessionPlanBuilder
{
    private SessionPlan _sessionPlan = new SessionPlan();

    public SessionPlanBuilder WithId(int id)
    {
        _sessionPlan.Id = id;
        return this;
    }

    public SessionPlanBuilder WithTitle(string title)
    {
        _sessionPlan.Title = title;
        return this;
    }

    public SessionPlanBuilder WithSummary(string summary)
    {
        _sessionPlan.Summary = summary;
        return this;
    }

    public SessionPlanBuilder WithCreatedDate(DateTime createdDate)
    {
        _sessionPlan.CreatedDate = createdDate;
        return this;
    }

    public SessionPlanBuilder WithClips(params Clip[] clips)
    {
        _sessionPlan.Clips = clips.ToList();
        return this;
    }

    public SessionPlanBuilder WithClip(Clip clip)
    {
        _sessionPlan.Clips.Add(clip);
        return this;
    }

    public SessionPlan Build() => _sessionPlan;
}

public class SettingBuilder
{
    private Setting _setting = new Setting();

    public SettingBuilder WithId(int id)
    {
        _setting.Id = id;
        return this;
    }

    public SettingBuilder WithKey(string key)
    {
        _setting.Key = key;
        return this;
    }

    public SettingBuilder WithValue(string value)
    {
        _setting.Value = value;
        return this;
    }

    public Setting Build() => _setting;
}

