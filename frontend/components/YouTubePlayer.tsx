'use client';

interface YouTubePlayerProps {
  videoId: string;
}

export default function YouTubePlayer({ videoId }: YouTubePlayerProps) {
  return (
    <div className="w-full aspect-video">
      <iframe
        className="w-full h-full rounded-lg"
        src={`https://www.youtube.com/embed/${videoId}`}
        title="YouTube video player"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowFullScreen
      />
    </div>
  );
}

