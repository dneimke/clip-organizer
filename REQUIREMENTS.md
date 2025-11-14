# 🏗️ Field Hockey Video Library MVP Prompt

Design and implement a Minimum Viable Product (MVP) for a field hockey video clip management system. The core goal is to enable rapid tagging, filtering, and retrieval of video content stored in two distinct locations: local Windows file paths and YouTube URLs.

## I. Technology Stack

- **Backend/API**: ASP.NET Core (C#) with an SQLite database for persistence.
- **Frontend/UI**: Next.js (React/TypeScript).

## II. Core Data Model

The database must store metadata only for each clip, including:

- Clip ID
- Title
- StorageType (Local or YouTube)
- LocationString (The local file path OR the YouTube URL/ID)
- Duration
- Tags (A flexible, searchable list of keywords).

## III. Key Features & Implementation Logic

### 1. Clip Ingestion & Tagging (Next.js & ASP.NET)

**Interface**: A single-page form allowing users to input either a local file path or a YouTube URL.

**Validation**: The ASP.NET API must validate the input type. For YouTube, it must use the YouTube Data API to retrieve the title and duration.

**Structured Tagging**: The Next.js UI must utilize controlled inputs (e.g., multi-select dropdowns/autocomplete) to enforce consistency for the following structured tag categories:

- **Skill/Tactic**: (e.g., Flick, PC Attack)
- **Field Area**: (e.g., Attacking D, Midfield)
- **Player Role**: (e.g., Defender, Goalie)
- **Outcome/Quality**: (e.g., Success, Error)

### 2. Search and Filtering (Next.js & ASP.NET)

The Next.js frontend must feature a prominent search bar and a sidebar that allows users to filter by multiple tags simultaneously (e.g., "Skill: Flick" AND "Outcome: Success").

The ASP.NET API must expose an efficient endpoint to handle these complex, multi-tag filtering queries against the SQLite database.

### 3. Playback Logic

**YouTube Clips**: The Next.js UI must embed an iFrame player for seamless, in-app playback using the stored video ID.

**Local Clips**: The Next.js UI cannot directly play the file. Instead, it must display the `file://...` path and provide a clear instruction or button for the user to manually open the file in their local Windows media player.

The focus must be on consistency, speed, and discoverability of the clips.
