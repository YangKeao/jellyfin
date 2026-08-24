# Jellyfin Anime4K integration

This branch adds server-controlled Anime4K upscaling to Jellyfin 10.11.11. Official Android TV and browser clients require no playback changes: eligible requests are converted to normal Jellyfin HLS transcodes by the server.

## Behavior

The administrator switch is under **Dashboard → Playback → Transcoding → Anime4K upscaling** and defaults to off. Changes apply to new playback sessions.

Anime4K is applied only when all of these conditions are true:

- NVIDIA NVENC is selected and the container startup Vulkan/libplacebo probe succeeded.
- The source is local VOD, SDR, 30 fps or below, and smaller than its 4K-boundary target.
- The item or parent series has genre `Animation`, `Anime`, `动画`, or `動畫`.
- The item or parent series is not tagged `NoAnime4K`.

The tag `Anime4K` forces metadata inclusion for titles with missing genres. `NoAnime4K` always wins. Output preserves aspect ratio within 3840×2160 (for example, 4:3 becomes 2880×2160). HDR and Dolby Vision are intentionally bypassed.

The video path is NVDEC → system memory → Vulkan/libplacebo Anime4K → subtitle composition → CUDA upload → NVENC. H.264 is capped at 35 Mbps and HEVC at 20 Mbps while respecting lower client/user limits. Version 1 supports one Anime4K stream at a time through the Kubernetes GPU limit.

## Build

```bash
docker buildx build \
  --platform linux/amd64 \
  --file Dockerfile.anime4k \
  --tag ghcr.io/yangkeao/anime4k-jellyfin:10.11.11-2 \
  .
```

The image pins Jellyfin 10.11.11, Jellyfin Web 10.11.11, and Anime4K 4.0.1. It installs Debian's Vulkan loader and EGL runtime because the FFmpeg-bundled loader does not initialize the RTX 2070 SUPER correctly in this deployment and NVIDIA's Vulkan ICD requires EGL at runtime.

## Runtime status

Administrators can query `GET /System/Anime4K/Status`. `Effective` is true only when configuration, NVENC, shader files, NVIDIA graphics libraries, Vulkan, and libplacebo are ready. A failed startup probe leaves normal Jellyfin playback untouched.

## Rollback

Roll back to official `jellyfin/jellyfin:10.11.11`, not 10.11.6, so the server database remains on the same schema version. The added encoding XML property is ignored by upstream Jellyfin.
