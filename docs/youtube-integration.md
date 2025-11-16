# YouTube Integration Guide

## Overview

The Clip Organizer application integrates with YouTube to allow users to store and play YouTube videos as clips alongside local video files. This integration uses the YouTube Data API v3 to fetch video metadata (title, duration) and embeds videos using YouTube's standard embed player.

## Features

- **Automatic Metadata Fetching**: When you add a YouTube URL, the system automatically retrieves the video title and duration
- **Multiple URL Format Support**: Accepts various YouTube URL formats (watch, short links, embed URLs, or direct video IDs)
- **Unified Interface**: YouTube videos are displayed alongside local video files in the same interface
- **Video ID Normalization**: Stores only the video ID (11 characters) for efficient storage

## Prerequisites

- A Google account
- Access to Google Cloud Console
- A YouTube Data API v3 key

## Step-by-Step: Getting a YouTube API Key

### Step 1: Create a Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Sign in with your Google account
3. Click on the project dropdown at the top of the page
4. Click **"New Project"**
5. Enter a project name (e.g., "Clip Organizer")
6. Click **"Create"**
7. Wait for the project to be created, then select it from the project dropdown

### Step 2: Enable YouTube Data API v3

1. In the Google Cloud Console, navigate to **"APIs & Services"** > **"Library"** (or use the search bar)
2. Search for **"YouTube Data API v3"**
3. Click on **"YouTube Data API v3"** from the results
4. Click the **"Enable"** button
5. Wait for the API to be enabled (this may take a few moments)

### Step 3: Create API Credentials

1. Navigate to **"APIs & Services"** > **"Credentials"** in the left sidebar
2. Click **"+ CREATE CREDENTIALS"** at the top of the page
3. Select **"API key"** from the dropdown menu
4. A new API key will be generated and displayed in a dialog box
5. **Important**: Copy the API key immediately - you won't be able to see it again in full
6. Click **"Close"** (you can restrict the key later if needed)

### Step 4: (Optional) Restrict API Key

For better security, you can restrict your API key:

1. In the **"Credentials"** page, click on your newly created API key
2. Under **"API restrictions"**, select **"Restrict key"**
3. Under **"Select APIs"**, choose **"YouTube Data API v3"**
4. Under **"Application restrictions"**, you can optionally:
   - Restrict by IP addresses (for server-side use)
   - Restrict by HTTP referrers (for web applications)
5. Click **"Save"**

**Note**: For development, you can skip restrictions, but it's recommended for production environments.

## Configuration

### Quick Reference: Choosing a Configuration Method

- **Development (Local)**: Use **.NET User Secrets** (recommended) - see section below
- **Development (Team)**: Use `appsettings.Development.json` (can be committed with placeholder values)
- **Production**: Use environment variables or secure secret management systems

### Backend Configuration

1. Open `backend/ClipOrganizer.Api/appsettings.json`
2. Locate the `"YouTube"` section:
   ```json
   "YouTube": {
     "ApiKey": "YOUR_YOUTUBE_API_KEY_HERE"
   }
   ```
3. Replace `"YOUR_YOUTUBE_API_KEY_HERE"` with your actual API key:
   ```json
   "YouTube": {
     "ApiKey": "AIzaSyBxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
   }
   ```

### Development Configuration (Optional)

If you want different settings for development:

1. Open `backend/ClipOrganizer.Api/appsettings.Development.json`
2. Add the YouTube configuration:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "YouTube": {
       "ApiKey": "YOUR_DEV_API_KEY_HERE"
     },
     "Gemini": {
       "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
       "Model": "gemini-1.5-flash"
     }
   }
   ```

**Note**: Settings in `appsettings.Development.json` override those in `appsettings.json` when running in Development mode.

### Using .NET User Secrets (Recommended for Development)

.NET User Secrets is the recommended way to store sensitive configuration data during development. It stores secrets outside your project tree, so they're never committed to version control.

#### Initial Setup

The project already has User Secrets configured. The `UserSecretsId` is defined in `backend/ClipOrganizer.Api/ClipOrganizer.Api.csproj`.

#### Setting the YouTube API Key

1. Navigate to the API project directory:
   ```bash
   cd backend/ClipOrganizer.Api
   ```

2. Set the YouTube API key using the `dotnet user-secrets` command:
   ```bash
   dotnet user-secrets set "YouTube:ApiKey" "your-api-key-here"
   ```

   Replace `your-api-key-here` with your actual YouTube API key.

#### Verifying the Secret

To verify the secret was set correctly:

```bash
dotnet user-secrets list
```

This will display all configured user secrets. You should see:
```
YouTube:ApiKey = your-api-key-here
```

#### Viewing a Specific Secret

To view just the YouTube API key:

```bash
dotnet user-secrets get "YouTube:ApiKey"
```

#### Removing a Secret

To remove the YouTube API key:

```bash
dotnet user-secrets remove "YouTube:ApiKey"
```

#### How User Secrets Work

- User secrets are stored in a JSON file on your local machine (outside the project directory)
- They are automatically loaded in **Development** mode by ASP.NET Core
- The configuration hierarchy is:
  1. User Secrets (highest priority in Development)
  2. `appsettings.Development.json`
  3. `appsettings.json`
  4. Environment variables

#### Location of User Secrets File

User secrets are stored in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

The `<UserSecretsId>` for this project is: `44f14146-418b-4609-aaaf-a9bbe60dabb0`

#### Benefits of User Secrets

- ✅ Never committed to version control
- ✅ Automatically loaded in Development mode
- ✅ Easy to manage via command line
- ✅ Per-developer configuration (each developer can have their own API key)
- ✅ No need to modify `appsettings.json` files

### Environment Variables (Alternative)

For production or more secure setups, you can use environment variables instead of storing the API key in configuration files:

1. Set the environment variable:
   - **Windows PowerShell**: `$env:YouTube__ApiKey = "your-api-key-here"`
   - **Windows CMD**: `set YouTube__ApiKey=your-api-key-here`
   - **Linux/Mac**: `export YouTube__ApiKey=your-api-key-here`

2. The application will automatically read from environment variables using the `YouTube:ApiKey` key format (double underscore `__` represents nested configuration in environment variables)

## How It Works

### Handling YouTube Clips

When a clip's location is set to a YouTube URL (e.g., during an update), the system:
1. Validates the URL format
2. Extracts the 11-character video ID
3. Fetches metadata via the YouTube Data API:
   - Video title
   - Video duration (seconds)
4. Stores normalized values:
   - `StorageType = YouTube`
   - `LocationString = videoId` (the 11-character ID, not the full URL)
   - `Title = video title` (or user-provided title if specified)
   - `Duration = duration in seconds`

### Supported URL Formats

The integration supports the following YouTube URL formats:

- `https://www.youtube.com/watch?v=VIDEO_ID`
- `https://youtu.be/VIDEO_ID`
- `https://www.youtube.com/embed/VIDEO_ID`
- `https://www.youtube.com/watch?feature=share&v=VIDEO_ID`
- Direct video ID: `VIDEO_ID` (11 characters)

### Displaying YouTube Clips

When viewing a clip with `StorageType = YouTube`:

1. The frontend detects the storage type
2. The `YouTubePlayer` component renders an iframe
3. The video is embedded using: `https://www.youtube.com/embed/{videoId}`
4. Users can play, pause, and control the video using YouTube's standard controls

## API Quotas and Limits

The YouTube Data API v3 has the following default quotas:

- **Queries per day**: 10,000 units (free tier)
- **Queries per 100 seconds per user**: 1,000 units

**Note**: Each video metadata request uses 1 unit. If you exceed the quota, you'll receive an error. Monitor your usage in the Google Cloud Console under "APIs & Services" > "Dashboard".

## Troubleshooting

### Error: "YouTube API key is not configured"

**Solution**: Ensure the API key is configured using one of these methods:
- **Development**: Use `dotnet user-secrets set "YouTube:ApiKey" "your-key"` (recommended)
- **Configuration file**: Set in `appsettings.json` or `appsettings.Development.json`
- **Environment variable**: Set `YouTube__ApiKey` environment variable

Verify the key is set by running `dotnet user-secrets list` (for user secrets) or checking your configuration files.

### Error: "Video with ID {videoId} not found"

**Possible causes**:
- The video has been deleted or made private
- The video ID is incorrect
- The video is region-restricted

**Solution**: Verify the video URL is correct and accessible.

### Error: "Invalid YouTube URL"

**Possible causes**:
- URL format is not supported
- URL is malformed

**Solution**: Use one of the supported URL formats listed above.

### Error: "API key not valid" or "API key expired"

**Solution**:
1. Verify the API key is correct in your configuration
2. Check that the YouTube Data API v3 is enabled in Google Cloud Console
3. Ensure the API key hasn't been deleted or restricted incorrectly
4. Check your API quota hasn't been exceeded

### Videos Not Playing

**Possible causes**:
- Video is age-restricted (requires YouTube's age verification)
- Video is blocked in your region
- Browser security settings blocking iframes

**Solution**: Test the video URL directly on YouTube to verify accessibility.

## Security Best Practices

1. **Never commit API keys to version control**: 
   - **Development**: Use `.NET User Secrets` (recommended) - automatically excluded from version control
   - **Production**: Use environment variables or secure secret management systems (Azure Key Vault, AWS Secrets Manager, etc.)
   - If using configuration files, ensure `appsettings.json` with real keys is in `.gitignore`

2. **Restrict API keys**: Limit API keys to specific APIs and IP addresses in production

3. **Monitor usage**: Regularly check API usage in Google Cloud Console

4. **Rotate keys**: If a key is compromised, delete it and create a new one

5. **Use separate keys**: Use different API keys for development and production environments

6. **Prefer User Secrets for Development**: User Secrets are the recommended approach for local development as they're automatically excluded from version control and easy to manage

## Related Files

- **Backend Service**: `backend/ClipOrganizer.Api/Services/YouTubeService.cs`
- **Service Interface**: `backend/ClipOrganizer.Api/Services/IYouTubeService.cs`
- **Controller**: `backend/ClipOrganizer.Api/Controllers/ClipsController.cs`
- **Frontend Component**: `frontend/components/YouTubePlayer.tsx`
- **Model**: `backend/ClipOrganizer.Api/Models/StorageType.cs`

## Additional Resources

- [YouTube Data API v3 Documentation](https://developers.google.com/youtube/v3)
- [Google Cloud Console](https://console.cloud.google.com/)
- [YouTube Data API Quotas](https://developers.google.com/youtube/v3/getting-started#quota)
- [API Key Best Practices](https://cloud.google.com/docs/authentication/api-keys)

