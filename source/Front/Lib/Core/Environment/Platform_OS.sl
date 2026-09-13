# =========================================================================
# Platform_OS — OS / osVersion / form definition enums (§13.4 / §13.5 / §13.6).
#
# Part of `namespace Environment { namespace Platform { ... } }`
# (design doc PLATFORM_CAPABILITY_DESIGN.md §13.2). Members are the single
# source of truth: member name == jsonc platform key; enum values are shared
# with CVM sl_environment.h / sl_platform_def.h (§13.9 consistency).
#
# Usage:
#   if Environment.current.os == Environment.Platform.os.window { ... }
#   if Environment.current.osVersion >= Environment.Platform.osVersion.window10 { ... }
# =========================================================================

namespace Environment
{
    namespace Platform
    {
        # Operating system classification (§13.4).
        # Member name == jsonc platform key (aliases accepted by CVM only).
        public enum os extends int
        {
            unknown = 0

            # Desktop / server
            # window aliases: windows / win / win32 / win64
            window  = 1
            linux   = 2
            # mac aliases: macos / osx / darwin
            mac     = 3
            # unix: other Unix-like
            unix    = 4
            # freeBSD alias: bsd
            freeBSD = 5

            # Mobile
            android = 10
            # ios aliases: iphone / ipad
            ios     = 11

            # Game consoles
            ps4            = 20
            ps5            = 21
            xboxOne        = 22
            xboxSeries     = 23
            nintendoSwitch = 24

            # Web / sandbox
            # browser: wasm browser host
            browser = 30
            wasi    = 31

            # Embedded
            # bareMetal: no OS
            bareMetal = 40
            # rtos: real-time OS
            rtos      = 41
        }

        # OS version detail (§13.5: numeric layer, for >= comparisons).
        public enum osVersion extends int
        {
            unknown = 0

            # Windows
            window7          = 100
            window8          = 101
            window10         = 102
            window11         = 103
            windowServer2019 = 110
            windowServer2022 = 111

            # Linux generic / distro
            linuxGeneric = 200
            linuxDebian  = 201
            linuxUbuntu  = 202
            linuxArch    = 203
            linuxAlpine  = 204
            linuxCentos  = 205
            linuxRhel    = 206
            linuxFedora  = 207
            linuxOpenSuse = 208
            linuxGentoo  = 209
            linuxKali    = 210

            # Linux embedded toolchain
            linuxKeil     = 250
            linuxYocto    = 251
            linuxBuildroot = 252
            linuxOpenWrt  = 253

            # macOS
            macos13 = 300
            macos14 = 301
            macos15 = 302

            # iOS
            ios16 = 400
            ios17 = 401
            ios18 = 402

            # Android API level
            androidApi31 = 500
            androidApi33 = 501
            androidApi34 = 502

            # Consoles SDK
            ps4Sdk   = 600
            ps5Sdk   = 601
            xboxGdk  = 602
            switchSdk = 603

            # WebAssembly
            wasiPreview1 = 700
            wasiP2       = 701
        }

        # Environment form (§13.6; §4 classification target).
        public enum form extends int
        {
            unknown   = 0
            desktop   = 1
            server    = 2
            mobile    = 3
            embedded  = 4
            web       = 5
            # console: game console
            console   = 6
            # container: docker / k8s
            container = 7
        }
    }
}
