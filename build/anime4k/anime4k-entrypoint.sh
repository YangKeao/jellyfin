#!/bin/sh

set -u

shader_path="${JELLYFIN_ANIME4K_SHADER:-/usr/share/anime4k/Anime4K_ModeA_Fast.glsl}"
system_vulkan="/usr/lib/x86_64-linux-gnu/libvulkan.so.1"
probe_log="/tmp/anime4k-probe.log"

if [ -f "$system_vulkan" ]; then
    LD_PRELOAD="${LD_PRELOAD:+${LD_PRELOAD}:}${system_vulkan}"
    export LD_PRELOAD
fi

JELLYFIN_ANIME4K_AVAILABLE=0
JELLYFIN_ANIME4K_REASON="Anime4K startup probe failed."

if [ ! -f "$shader_path" ]; then
    JELLYFIN_ANIME4K_REASON="Anime4K shader bundle is missing."
elif /usr/lib/jellyfin-ffmpeg/ffmpeg \
    -v error \
    -init_hw_device vulkan=vk:0 \
    -filter_hw_device vk \
    -f lavfi \
    -i color=size=64x64:rate=1 \
    -vf "format=yuv420p,hwupload,libplacebo=w=128:h=128:custom_shader_path=${shader_path},hwdownload,format=yuv420p" \
    -frames:v 1 \
    -f null - >"$probe_log" 2>&1; then
    JELLYFIN_ANIME4K_AVAILABLE=1
    JELLYFIN_ANIME4K_REASON="Anime4K Vulkan/libplacebo startup probe succeeded."
else
    last_error="$(tail -n 1 "$probe_log" 2>/dev/null || true)"
    if [ -n "$last_error" ]; then
        JELLYFIN_ANIME4K_REASON="Anime4K startup probe failed: ${last_error}"
    fi
fi

export JELLYFIN_ANIME4K_AVAILABLE
export JELLYFIN_ANIME4K_REASON
rm -f "$probe_log"

exec /jellyfin/jellyfin "$@"
